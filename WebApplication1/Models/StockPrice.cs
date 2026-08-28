using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class StockPrice
{
    public int StockPriceId { get; set; }

    [Required, StringLength(20)]
    public string StockCode { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Ticker { get; set; } = string.Empty;

    public DateOnly PriceDate { get; set; }
    public decimal Close { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
