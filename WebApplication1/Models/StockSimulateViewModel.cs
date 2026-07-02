namespace WebApplication1.Models
{
    public class StockSimulateViewModel
    {
        // 輸入
        public int FundPoolId { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;

        // 輸出
        public decimal TotalInvested { get; set; }       // 總入金
        public decimal TotalWithdrawn { get; set; }      // 總出金
        public int TotalShares { get; set; }             // 目前持股數
        public decimal TodayPrice { get; set; }          // 今日股價
        public decimal CurrentValue { get; set; }        // 目前市值（持股 × 今日股價）
        public decimal TotalReturn { get; set; }         // 總報酬（現金 + 市值 - 總入金）
        public decimal ReturnRate { get; set; }          // 報酬率
        public string TodayDate { get; set; } = string.Empty;  // 今日日期
    }
}