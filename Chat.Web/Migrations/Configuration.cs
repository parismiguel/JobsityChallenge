namespace Chat.Web.Migrations
{
    using Chat.Web.Helpers;
    using Chat.Web.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {

            if (!context.Users.Any(u => u.UserName == "Administrador"))
            {
                var store = new UserStore<ApplicationUser>(context);
                var manager = new UserManager<ApplicationUser>(store);

                ApplicationUser admin = new ApplicationUser
                {
                    UserName = "Administrador",
                    DisplayName = "bot",
                    Avatar = StaticResources.Avatars[4],
                    Email = "admin@jobsity.com"
                };

                IdentityResult result = manager.Create(admin, "Jobsity2019");

                if (result.Succeeded == false)
                {
                    throw new Exception(result.Errors.First());
                }
            }
        }
    }
}
