using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs;
using ShopifySync_DataAccessLayer.Enum;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FitmentController : ControllerBase
    {
        private readonly IFitmentService _fitmentService;
        private readonly ICommonService _commonService;
        private readonly IShopifyService _shopifyService;
        private readonly ILogger<FitmentController> _logger;
        private readonly ResponseMessageList _apiResponseMessageList = new ResponseMessageList();

        public FitmentController(
            IFitmentService fitmentService, 
            ICommonService commonService,
            IShopifyService shopifyService,
            ILogger<FitmentController> logger)
        {
            _fitmentService = fitmentService;
            _commonService = commonService;
            _shopifyService = shopifyService;
            _logger = logger;
        }

        [HttpPost("upsert")]
        public async Task<ActionResult<Response>> UpsertFitmentData([FromBody] FitmentUpsertDTO request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                if (string.IsNullOrEmpty(request.Mode))
                {
                    return BadRequest("Mode is required.");
                }

                if (request.Mode == "by_variant" && string.IsNullOrEmpty(request.VariantId))
                {
                    return BadRequest("Variant ID is required when mode is 'by_variant'.");
                }

                if (request.Mode == "by_product" && string.IsNullOrEmpty(request.ProductId))
                {
                    return BadRequest("Product ID is required when mode is 'by_product'.");
                }

                if (request.Mode != "by_variant" && request.Mode != "by_product" && request.Mode != "all_variants")
                {
                    return BadRequest("Invalid mode. Must be 'by_variant', 'by_product', or 'all_variants'.");
                }

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation("[FitmentController] Sending FitmentSync to queue with mode: {Mode}", request.Mode);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.FitmentSync.ToString());
                
                return Ok(ResponseHelper.Success("Fitment sync request added to queue successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FitmentController] Error in UpsertFitmentData: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertFitmentData", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
