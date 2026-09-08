using Microsoft.AspNetCore.Mvc;
using ShopifySync_BusinessLogicLayer.Infrastructure.Job.Background;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopifyController : ControllerBase
    {
        private readonly ImportProductsBackgroundService _importProductsBackgroundService;
        private readonly IShopifyService _shopifyService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public ShopifyController(ImportProductsBackgroundService importProductsBackgroundService, IShopifyService shopifyService, ICommonService commonService)
        {
            _importProductsBackgroundService = importProductsBackgroundService;
            _shopifyService = shopifyService;
            _commonService = commonService;
        }

        [HttpPost("TriggerImportProducts")]
        public Task<IActionResult> TriggerImportProducts(DateTime? date = null)
        {
            try
            {
                if (_importProductsBackgroundService.IsRunning)
                {
                    return Task.FromResult<IActionResult>(Conflict("ImportProducts job is already running."));
                }
                _importProductsBackgroundService.TriggerImport(date);
                return Task.FromResult<IActionResult>(Ok("ImportProducts background job triggered."));
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "TriggerImportProducts", 1, ex.Message, ex.ToString());
                return Task.FromResult<IActionResult>(BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage)));
            }
        }

        [HttpPost("ImportProductById/{productId}")]
        public async Task<IActionResult> ImportProductByIdAsync(long productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return BadRequest("Request body is Invalid.");
                }
                var productImportStatus = await _shopifyService.ImportProductByIdAsync(productId);
                if (productImportStatus == null)
                {
                    return NotFound($"Product with ID {productId} does not exist in Shopify.");
                }
                if (productImportStatus.Contains("401"))
                {
                    return Unauthorized(productImportStatus);
                }
                return Ok(productImportStatus);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductByIdAsync", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
