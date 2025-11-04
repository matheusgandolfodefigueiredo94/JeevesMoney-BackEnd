using JeevesMoney.Domain.Entities;
using System.Threading.Tasks;

namespace JeevesMoney.Application.Interfaces
{
 public interface IStockService
 {
 Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
 }
}