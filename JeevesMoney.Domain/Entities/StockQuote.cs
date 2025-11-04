namespace JeevesMoney.Domain.Entities
{
 public class StockQuote
 {
 public string Symbol { get; set; } = string.Empty;
 public decimal Price { get; set; }
 public string? Currency { get; set; }
 public DateTime? Timestamp { get; set; }
 }
}