using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Identity
{
    // Renamed to avoid collision with Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext
    public class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

       

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 🔥 КРИТИЧЕСКИ ВАЖНО для работы OpenIddict с EF Core
            builder.UseOpenIddict();
        }

    }
}