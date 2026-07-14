using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User   //使用者模型，對應資料庫中的Users表
    {
        [Key]
        public int Id { get; set; } //自動變PK
        [Required(ErrorMessage = "請輸入帳號")]
        [RegularExpression (@"^[a-zA-Z0-9]{4,20}$", ErrorMessage = "帳號必須是4到20個字母或數字")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "請輸入密碼")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼至少8個字元，且必須包含至少一個字母和一個數字")]
        public string Password { get; set; } = string.Empty;

        public ICollection<FundPool> FundPools { get; set; } = new List<FundPool>(); //一個使用者可以有多個基金池
    }
}
