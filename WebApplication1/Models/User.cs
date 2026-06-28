using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User   //使用者模型，對應資料庫中的Users表
    {
        [Key]
        public int Id { get; set; } //自動變PK
        [Required(ErrorMessage = "請輸入帳號")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        public ICollection<FundPool> FundPools { get; set; } = new List<FundPool>(); //一個使用者可以有多個基金池
    }
}
