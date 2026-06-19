using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 這裡以後可以放你的資料表對應，例如：
        public DbSet<User> Users { get; set; }
    }
}
