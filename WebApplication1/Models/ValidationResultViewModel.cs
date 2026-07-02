namespace WebApplication1.Models
{
    public class ValidationError
    {
        public DateTime TransactionTime { get; set; }
        public TransactionType Type { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ValidationResultViewModel
    {
        public int FundPoolId { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string RedirectTo { get; set; } = string.Empty;  // 加這行
        public List<ValidationError> Errors { get; set; } = new();
        public decimal FinalCash { get; set; }
        public Dictionary<string, int> FinalStocks { get; set; } = new();
    }
}
