using AutoMapper;
using Chat.Web.Mappings;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Chat.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<MessageProfile>();
                cfg.AddProfile<RoomProfile>();
                cfg.AddProfile<UserProfile>();
            });

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
