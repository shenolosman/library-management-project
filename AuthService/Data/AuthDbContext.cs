using AuthService.Model;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> opt) : base(opt) { }
        public DbSet<User> Users => Set<User>();

    }
}