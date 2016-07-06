using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace FrontierVOps.ChannelMapWS
{
    public static class WebApiConfig
    {
        public static string ConnectionString { get; set; }

        static WebApiConfig()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
#if DEBUG
                ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["FIOSApp_DC_DEBUG"].ConnectionString;
#else
                ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["FiosAppDCTrusted"].ConnectionString;
#endif
            }
        }

        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Filters.Add(new AuthorizeAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "ActionBased",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
