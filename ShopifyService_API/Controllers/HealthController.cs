using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartFinder_DataAccess.Context;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly PartFinderDbContext _dbContext;
        private readonly ILogger<HealthController> _logger;

        public HealthController(PartFinderDbContext dbContext, ILogger<HealthController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();

                if (!canConnect)
                {
                    _logger.LogWarning("ShopifyService health check: Database connection failed");
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        service = "ShopifyService",
                        reason = "Database connection failed",
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    status = "healthy",
                    service = "ShopifyService",
                    database = "connected",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShopifyService health check failed with exception");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    service = "ShopifyService",
                    reason = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
