using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Models;

public class FundPool
{
    [Key]
    public int FundPoolId { get; set; }

    // 外鍵(User)
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    [Required]
    [StringLength(50)]
    public string PoolName { get; set; } = string.Empty;

    // 目前市值
    public decimal CurrentValue { get; set; }

    // 建立時間
    public DateTime CreateTime { get; set; } = DateTime.Now;

    // 一個資金池有很多交易紀錄
    public ICollection<FundTransaction> Transactions { get; set; } = new List<FundTransaction>();
}