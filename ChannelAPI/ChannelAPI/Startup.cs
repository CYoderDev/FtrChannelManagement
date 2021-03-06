﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace ChannelAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("project.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Build cors policy
            var corsBuilder = new CorsPolicyBuilder();
            corsBuilder.AllowCredentials();
            corsBuilder.AllowAnyMethod();
            corsBuilder.AllowAnyOrigin();
            corsBuilder.AllowAnyHeader();

            // Add framework services.
            services.AddMvc(options =>
            {
                options.InputFormatters.Insert(0, new ImageFormatter());
            });

            services.AddDirectoryBrowser();

            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.Configure<IISOptions>(options =>
            {
                options.AutomaticAuthentication = true;
            });
            services.AddAuthentication(Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme);
            services.AddCors(options =>
            {
                options.AddPolicy("ChannelAPICorsPolicy", corsBuilder.Build());
            });

            services.AddAuthorization(options =>
            {
#if DEBUG
                options.AddPolicy("RequireWindowsGroupMembership", policy => policy.RequireRole(@"CORP\FTW Data Center", @"VHE\FUI-IMG"));
#else
                options.AddPolicy("RequireWindowsGroupMembership", policy => policy.RequireRole(@"VHE\FUI-IMG"));
#endif
            });

#if DEBUG
            DapperFactory.ConnectionString = Configuration.GetConnectionString("FIOSAPP_DC_DEBUG");
#else
            DapperFactory.ConnectionString = Configuration.GetConnectionString("FIOSAPP_DC");
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            //app.UseStaticFiles();

            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials().Build())
            .Use(async (context, next) =>
                {
                    await next();
                    if (context.Response.StatusCode == 404 &&
                        !System.IO.Path.HasExtension(context.Request.Path.Value) &&
                        !context.Request.Path.Value.StartsWith("/api/"))
                    {
                        context.Request.Path = "/index.html";
                        await next();
                    }
                })
            .UseStaticFiles(new StaticFileOptions() {
                OnPrepareResponse = (context) =>
                {
                    //Set cache control to no-cache in the header on logo calls which means 
                    //the browser must revalidate whenever the image has changed.
                    if (context.File != null && context.File.Name.EndsWith(".png"))
                    {
                        context.Context.Response.Headers.Add("Cache-Control", "no-cache");
                    }
                }
            })
            .UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"wwwroot", "ChannelLogoRepository")),
                RequestPath = new PathString("/ChannelLogoRepository")
            })
            .UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                        System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"wwwroot", "ChannelLogoRepository")),
                RequestPath = new PathString("/ChannelLogoRepository")
            })
            .UseDefaultFiles()
            .UseMvcWithDefaultRoute();


            loggerFactory.AddNLog();
            app.AddNLogWeb();

            env.ConfigureNLog("nlog.config");

            if (env.IsDevelopment())
            {
                app.UseBrowserLink()
                .UseDeveloperExceptionPage()
                .UseStatusCodePages();
            }
            else
            {
                app.UseExceptionHandler();
            }
            
            //app.UseDefaultFiles();
            //app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
