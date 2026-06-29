using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public enum TransactionType
    {
        入金,
        出金,
        買入,
        賣出
    }
    public class FundTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int FundPoolId { get; set; }
        [ForeignKey(nameof(FundPoolId))]
        public required FundPool FundPool { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public DateTime TransactionTime { get; set; }

        // 入金出金用
        public decimal? Amount { get; set; }

        // 買入賣出用
        public string? StockCode { get; set; }
        public int? Shares { get; set; }
        public decimal? PricePerShare { get; set; }
    }
}
