namespace WebApplication1.Models
{
    public class StockHolding
    {
        public string StockCode { get; set; } = string.Empty;
        public int Shares { get; set; }
        public decimal TodayPrice { get; set; }
        public decimal MarketValue { get; set; }  // Shares × TodayPrice
    }

    public class ReturnViewModel
    {
        public int FundPoolId { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public decimal TotalInvested { get; set; }      // 總入金
        public decimal CashBalance { get; set; }        // 現金餘額
        public decimal TotalMarketValue { get; set; }   // 總持股市值
        public decimal TotalReturn { get; set; }        // 總報酬
        public decimal ReturnRate { get; set; }         // 報酬率
        public List<StockHolding> Holdings { get; set; } = new();
        public string PriceDate { get; set; } = string.Empty;  // 股價日期
    }
}