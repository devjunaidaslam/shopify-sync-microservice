using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using ShopifySync_DataAccessLayer.Enum;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopifyUpdateController : ControllerBase
    {
        private readonly ICommonService _commonService;
        private readonly IShopifyService _shopifyService;
        private readonly ILogger<ShopifyUpdateController> _logger;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public ShopifyUpdateController(
            ICommonService commonService,
            IShopifyService shopifyService,
            ILogger<ShopifyUpdateController> logger)
        {
            _commonService = commonService;
            _shopifyService = shopifyService;
            _logger = logger;
        }

        [HttpPut("UpdateVariantPrice")]
        public async Task<IActionResult> UpdateVariantPrices([FromBody] UpdateVariantPricesRequest request)
        {
            try
            {
                if (request == null
                    || request.VariantId == 0
                    || !decimal.TryParse(request.NewPrice.ToString(), out _)
                    || !decimal.TryParse(request.NewCompareAtPrice.ToString(), out _))
                {
                    return BadRequest("Request body is Invalid.");
                }

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation(
                    "Enqueueing VariantPriceUpdate for variant {VariantId}",
                    request.VariantId);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.VariantPriceUpdate.ToString());
                return Ok("Data added to Queue");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantPrices", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        [HttpPut("UpdateVariantLocationPrice")]
        public async Task<IActionResult> UpdateVariantLocationPrice([FromBody] UpdateVariantLocationPriceRequest request)
        {
            try
            {
                if (request == null
                    || request.VariantId == 0
                    || request.LocationPricePairs.Any(x => !decimal.TryParse(x.NewPrice.ToString(), out _))
                    || request.LocationPricePairs.Any(x => !decimal.TryParse(x.LocationId.ToString(), out _)))
                {
                    return BadRequest("Request body is Invalid.");
                }

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation(
                    "Enqueueing VariantLocationUpdate for variant {VariantId}",
                    request.VariantId);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.VariantLocationUpdate.ToString());
                return Ok("Data added to Queue");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantLocationPrice", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        [HttpPut("UpdateInventoryLevels")]
        public async Task<IActionResult> UpdateInventoryLevels([FromBody] UpdateInventoryRequest request)
        {
            try
            {
                if (request?.InventoryItems == null || !request.InventoryItems.Any())
                {
                    _commonService.ErrorLogs(
                        "Invalid request body",
                        "UpdateInventoryLevels",
                        1,
                        "Request is null or contains no inventory items",
                        "BadRequest");
                    return BadRequest("Request body is invalid or contains no inventory items.");
                }

                foreach (var item in request.InventoryItems)
                {
                    if (item.LocationId <= 0 || item.VariantId <= 0 || item.AvailableQuantity < 0)
                    {
                        _commonService.ErrorLogs(
                            "Invalid inventory item data",
                            "UpdateInventoryLevels",
                            1,
                            $"Invalid data for item: LocationId={item.LocationId}, VariantId={item.VariantId}, AvailableQuantity={item.AvailableQuantity}",
                            "BadRequest");
                        return BadRequest(
                            $"Invalid data for inventory item: LocationId={item.LocationId}, VariantId={item.VariantId}, AvailableQuantity={item.AvailableQuantity}");
                    }
                }

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation(
                    "Enqueueing InventoryLevelUpdate for {Count} items",
                    request.InventoryItems.Count);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.InventoryLevelUpdate.ToString());
                return Ok("Data added to Queue");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateInventoryLevels", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
