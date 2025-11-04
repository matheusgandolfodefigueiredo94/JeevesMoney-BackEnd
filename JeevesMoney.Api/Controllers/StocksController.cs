using JeevesMoney.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JeevesMoney.Api.Controllers
{
 [ApiController]
 [Route("api/[controller]")]
 public class StocksController : ControllerBase
 {
 private readonly IStockService _stockService;
 public StocksController(IStockService stockService)
 {
 _stockService = stockService;
 }

 [HttpGet("{symbol}")]
 public async Task<IActionResult> Get(string symbol, CancellationToken cancellationToken)
 {
 if (string.IsNullOrWhiteSpace(symbol)) return BadRequest("Symbol is required");
 var quote = await _stockService.GetQuoteAsync(symbol, cancellationToken);
 if (quote == null) return NotFound();
 return Ok(quote);
 }
 }
}