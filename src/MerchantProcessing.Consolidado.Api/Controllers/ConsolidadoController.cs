using Microsoft.AspNetCore.Mvc;
using MerchantProcessing.Domain.DTOs;
using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MerchantProcessing.Consolidado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsolidadoController : ControllerBase
{
    private readonly MerchantDbContext _context;
    private readonly ICacheService _cacheService;

    public ConsolidadoController(MerchantDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    [HttpGet("saldo/atual")]
    public async Task<ActionResult<BalanceResponse>> GetCurrentBalance([FromQuery] Guid accountId)
    {
        // Try cache first
        var cacheKey = $"account:{accountId}:balance";
        var cached = await _cacheService.GetAsync<BalanceResponse>(cacheKey);
        if (cached != null)
            return Ok(cached);

        // Query database
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
            return NotFound();

        var response = new BalanceResponse
        {
            AccountId = account.Id,
            Balance = account.Balance,
            LastUpdated = account.LastUpdated
        };

        // Cache for 60 seconds
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromSeconds(60));

        return Ok(response);
    }

    [HttpGet("diario/{date}")]
    public async Task<ActionResult<ConsolidatedDailyResponse>> GetDailyConsolidated(DateTime date, [FromQuery] Guid accountId)
    {
        // Try cache
        var cacheKey = $"account:{accountId}:consolidated:{date:yyyy-MM-dd}";
        var cached = await _cacheService.GetAsync<ConsolidatedDailyResponse>(cacheKey);
        if (cached != null)
            return Ok(cached);

        // Query consolidated daily table
        var consolidated = await _context.ConsolidatedDaily
            .Where(c => c.AccountId == accountId && c.Date.Date == date.Date)
            .FirstOrDefaultAsync();

        if (consolidated == null)
            return NotFound();

        var response = new ConsolidatedDailyResponse
        {
            Date = consolidated.Date,
            OpeningBalance = consolidated.OpeningBalance,
            TotalCredits = consolidated.TotalCredits,
            TotalDebits = consolidated.TotalDebits,
            ClosingBalance = consolidated.ClosingBalance,
            TransactionCount = consolidated.TransactionCount
        };

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "consolidado-api" });
    }
}
