using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            services.AddSingleton<IConfiguration>(Configuration);

            services.Configure<IISOptions>(options =>
            {
                options.ForwardWindowsAuthentication = true;
            });

            //Add Cors
            services.AddCors(options =>
            {
                options.AddPolicy("ChannelAPICorsPolicy", corsBuilder.Build());
            });

            services.AddAuthorization();
#if DEBUG
            DapperFactory.ConnectionString = Configuration.GetConnectionString("FIOSAPP_DC_DEBUG");
#else
            DapperFactory.ConnectionString = Configuration.GetConnectionString("FIOSAPP_DC");
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }
            else
            {
                app.UseExceptionHandler();
            }

            app.UseMvc();
        }
    }
}
