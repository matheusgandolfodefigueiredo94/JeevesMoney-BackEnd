using JeevesMoney.Application.Interfaces;
using JeevesMoney.Domain.Entities;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JeevesMoney.Infrastructure.Services
{
 public class BrapiStockService : IStockService
 {
 private readonly HttpClient _httpClient;
 private readonly string _quoteEndpointTemplate;

 public BrapiStockService(HttpClient httpClient)
 {
 _httpClient = httpClient;
 _quoteEndpointTemplate = "/api/quote/{symbol}"; // default; can be changed via configuration if needed
 }

 public async Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
 {
 if (string.IsNullOrWhiteSpace(symbol)) return null;

 var endpoint = _quoteEndpointTemplate.Replace("{symbol}", Uri.EscapeDataString(symbol));

 using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
 if (!response.IsSuccessStatusCode) return null;

 using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
 using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

 var root = doc.RootElement;
 JsonElement item;

 if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() >0)
 {
 item = results[0];
 }
 else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() >0)
 {
 item = data[0];
 }
 else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() >0)
 {
 item = root[0];
 }
 else if (root.TryGetProperty("stock", out var stock) && stock.ValueKind == JsonValueKind.Object)
 {
 item = stock;
 }
 else if (root.TryGetProperty("quote", out var quote) && quote.ValueKind == JsonValueKind.Object)
 {
 item = quote;
 }
 else
 {
 item = root;
 }

 var quoteEntity = new StockQuote
 {
 Symbol = item.GetPropertyOrDefault("symbol", string.Empty),
 Price = item.GetPropertyOrDefault("price",0m),
 Currency = item.GetPropertyOrDefault<string?>("currency", null),
 Change = item.GetPropertyOrDefault("regularMarketChange", 0m),
 ChangePercent = item.GetPropertyOrDefault("regularMarketChangePercent", 0m),
 };

 // Try common alternative property names
 if (quoteEntity.Price ==0m)
 {
 quoteEntity.Price = item.GetPropertyOrDefault("regularMarketPrice", quoteEntity.Price);
 quoteEntity.Price = item.GetPropertyOrDefault("lastPrice", quoteEntity.Price);
 quoteEntity.Price = item.GetPropertyOrDefault("close", quoteEntity.Price);
 }

 // Timestamp handling - try numeric unix time or ISO date string
 if (item.TryGetProperty("timestamp", out var t) && t.ValueKind == JsonValueKind.Number)
 {
 if (t.TryGetInt64(out var ts)) quoteEntity.Timestamp = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
 }
 else if (item.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String)
 {
 if (DateTime.TryParse(d.GetString(), out var dt)) quoteEntity.Timestamp = dt;
 }

 return quoteEntity;
 }
 }

 // Note: JsonExtensions is defined in YahooFinanceStockService.cs in the same namespace and assembly and will be used by this service as well.
}
