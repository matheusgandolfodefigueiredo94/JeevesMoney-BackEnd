using JeevesMoney.Application.Interfaces;
using JeevesMoney.Domain.Entities;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JeevesMoney.Infrastructure.Services
{
 public class YahooFinanceStockService : IStockService
 {
 private readonly HttpClient _httpClient;
 public YahooFinanceStockService(HttpClient httpClient)
 {
 _httpClient = httpClient;
 }

 public async Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
 {
 if (string.IsNullOrWhiteSpace(symbol)) return null;

 // Yahoo Finance unofficial API endpoint returning JSON
 var url = $"/v7/finance/quote?symbols={Uri.EscapeDataString(symbol)}";
 using var response = await _httpClient.GetAsync(url, cancellationToken);
 if (!response.IsSuccessStatusCode) return null;

 using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
 using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

 var root = doc.RootElement;
 if (!root.TryGetProperty("quoteResponse", out var quoteResponse)) return null;
 if (!quoteResponse.TryGetProperty("result", out var results)) return null;
 if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() ==0) return null;

 var item = results[0];
 var quote = new StockQuote
 {
 Symbol = item.GetPropertyOrDefault("symbol", string.Empty),
 Price = item.GetPropertyOrDefault("regularMarketPrice",0m),
 Currency = item.GetPropertyOrDefault<string?>("currency", null),
 Timestamp = item.TryGetProperty("regularMarketTime", out var t) && t.ValueKind == JsonValueKind.Number
 ? DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).DateTime
 : (DateTime?)null
 };

 return quote;
 }
 }

 internal static class JsonExtensions
 {
 public static T GetPropertyOrDefault<T>(this JsonElement element, string propertyName, T defaultValue)
 {
 if (!element.TryGetProperty(propertyName, out var prop)) return defaultValue;
 try
 {
 if (typeof(T) == typeof(string))
 {
 var s = prop.ValueKind == JsonValueKind.Null ? null : prop.GetString();
 return (T)(object?)s ?? defaultValue!;
 }
 if (typeof(T) == typeof(decimal))
 {
 if (prop.ValueKind == JsonValueKind.Number)
 {
 if (prop.TryGetDecimal(out var d)) return (T)(object)d;
 if (prop.TryGetDouble(out var dd)) return (T)(object)Convert.ToDecimal(dd);
 }
 return defaultValue;
 }
 if (typeof(T) == typeof(long))
 {
 if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var l)) return (T)(object)l;
 return defaultValue;
 }
 return JsonSerializer.Deserialize<T>(prop.GetRawText())!;
 }
 catch
 {
 return defaultValue;
 }
 }
 }
}
