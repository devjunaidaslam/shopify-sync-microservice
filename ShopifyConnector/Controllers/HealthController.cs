using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartFinder_DataAccess.Context;

namespace ShopifyConnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IDbContextFactory<PartFinderDbContext> _dbContextFactory;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IDbContextFactory<PartFinderDbContext> dbContextFactory, ILogger<HealthController> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var canConnect = await dbContext.Database.CanConnectAsync();

                if (!canConnect)
                {
                    _logger.LogWarning("ShopifyConnector health check: Database connection failed");
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        service = "ShopifyConnector",
                        reason = "Database connection failed",
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    status = "healthy",
                    service = "ShopifyConnector",
                    database = "connected",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShopifyConnector health check failed with exception");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    service = "ShopifyConnector",
                    reason = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
