using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Services
{
    public class StockPriceResult   // 取得今日股價的結果
    {
        [JsonPropertyName("stock_code")]
        public string StockCode { get; set; } = string.Empty;

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("close")]
        public decimal Close { get; set; }
    }

    public class BatchPriceItem // 取得多個日期股價的結果
    {
        [JsonPropertyName("requested_date")]
        public string RequestedDate { get; set; } = string.Empty;

        [JsonPropertyName("actual_date")]
        public string? ActualDate { get; set; }

        [JsonPropertyName("close")]
        public decimal? Close { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class BatchPriceResult// 取得多個日期股價的結果
    {
        [JsonPropertyName("stock_code")]
        public string StockCode { get; set; } = string.Empty;

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("prices")]
        public List<BatchPriceItem> Prices { get; set; } = new();
    }
    public class HistoryPriceResult // 取得指定日期股價的結果
    {
        [JsonPropertyName("stock_code")]
        public string StockCode { get; set; } = string.Empty;

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("requested_date")]
        public string RequestedDate { get; set; } = string.Empty;

        [JsonPropertyName("actual_date")]
        public string ActualDate { get; set; } = string.Empty;

        [JsonPropertyName("close")]
        public decimal Close { get; set; }
    }
    public class TodayBatchItem // 取得多支股票今日股價的結果
    {
        [JsonPropertyName("stock_code")]
        public string StockCode { get; set; } = string.Empty;

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("close")]
        public decimal? Close { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class TodayBatchResult   //取得多支股票今日股價的結果
    {
        [JsonPropertyName("prices")]
        public List<TodayBatchItem> Prices { get; set; } = new();
    }

    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        // 從設定檔讀取 BaseUrl
        public StockService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["StockApi:BaseUrl"]!;
        }

        // 取得今日股價
        public async Task<StockPriceResult?> GetTodayPriceAsync(string stockCode)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/price/today?stock_code={stockCode}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<StockPriceResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        // 取得指定日期股價
        public async Task<HistoryPriceResult?> GetHistoryPriceAsync(string stockCode, string date)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/price/history?stock_code={stockCode}&date={date}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HistoryPriceResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        // 一次取得多個日期股價
        public async Task<BatchPriceResult?> GetBatchPricesAsync(string stockCode, List<string> dates)
        {
            var datesParam = string.Join(",", dates);
            var response = await _httpClient.GetAsync($"{_baseUrl}/price/batch?stock_code={stockCode}&dates={datesParam}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BatchPriceResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        // 一次取得多支股票今日股價
        public async Task<TodayBatchResult?> GetTodayBatchPricesAsync(List<string> stockCodes)
        {
            var codesParam = string.Join(",", stockCodes);
            var response = await _httpClient.GetAsync($"{_baseUrl}/price/today/batch?stock_codes={codesParam}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodayBatchResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}