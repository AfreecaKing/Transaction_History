namespace WebApplication1.Models
{
    public class User   //使用者模型，對應資料庫中的Users表
    {
        public int Id { get; set; } //自動變PK
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
