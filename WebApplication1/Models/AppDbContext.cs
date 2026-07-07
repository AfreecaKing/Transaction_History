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
        public DbSet<FundPool> FundPools { get; set; }
        public DbSet<FundTransaction> FundTransactions { get; set; }

        // 手動新增這個覆寫方法，強制指定資料庫內對應的「小寫」資料表名稱
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 強制讓 C# 的 Users 對應到 Aiven Linux 上的小寫 users 資料表
            modelBuilder.Entity<User>().ToTable("users");

            // 同理，其他資料表也強制對應小寫
            modelBuilder.Entity<FundPool>().ToTable("fundpools");
            modelBuilder.Entity<FundTransaction>().ToTable("fundtransactions");
        }
    }

}
