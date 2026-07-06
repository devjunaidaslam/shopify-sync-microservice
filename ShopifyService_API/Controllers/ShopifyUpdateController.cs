using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using PartFinderMicroServices_DataAccessLayer.Enum;

namespace ShopifyConnector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopifyUpdateController : ControllerBase
    {
        private readonly IShopifyUpdateService _shopifyUpdateService;
        private readonly ICommonService _commonService;
        private readonly IShopifyService _shopifyService;
        private readonly ILogger<ShopifyUpdateController> _logger;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyUpdateController"/> class.
        /// </summary>
        /// <param name="shopifyUpdateService">Service for updating Shopify variants and prices.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        /// <param name="shopifyService">Shopify service that orchestrates RabbitMQ operations.</param>
        /// <param name="logger">Logger for application logging.</param>
        public ShopifyUpdateController(IShopifyUpdateService shopifyUpdateService, ICommonService commonService, IShopifyService shopifyService, ILogger<ShopifyUpdateController> logger)
        {
            _shopifyUpdateService = shopifyUpdateService;
            _commonService = commonService;
            _shopifyService = shopifyService;
            _logger = logger;
        }

        /// <summary>
        /// Updates the prices of Shopify variants in bulk.
        /// </summary>
        /// <param name="request">The request containing variant price update information.</param>
        /// <returns>Status of the update or an error response.</returns>
        [HttpPut("UpdateVariantPrice")]
        public async Task<IActionResult> UpdateVariantPrices([FromBody] UpdateVariantPricesRequest request)
        {
            try
            {
                if (request == null || request.VariantId == 0 || !decimal.TryParse(request.NewPrice.ToString(), out decimal newPrice)  || !decimal.TryParse(request.NewCompareAtPrice.ToString(), out decimal newCompareAtPrice) )
                {
                    return BadRequest("Request body is Invalid.");
                }

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation("[ShopifyUpdateController] Sending VariantPriceUpdate to queue for variant ID: {VariantId}", request.VariantId);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.VariantPriceUpdate.ToString());
                return Ok("Data added to Queue");
                //var responseContent = await _shopifyUpdateService.UpdateVariantPricesAsync(request);

                //return Ok(responseContent);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantPrices", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Updates the prices of Shopify variant locations in bulk.
        /// </summary>
        /// <param name="request">The request containing variant location price update information.</param>
        /// <returns>Status of the update or an error response.</returns>
        [HttpPut("UpdateVariantLocationPrice")]
        public async Task<IActionResult> UpdateVariantLocationPrice([FromBody] UpdateVariantLocationPriceRequest request)
        {
            try
            {
                if (request == null || request.VariantId == 0 || request.LocationPricePairs.Any(x =>  !decimal.TryParse(x.NewPrice.ToString(), out decimal newPrice)) || request.LocationPricePairs.Any(x => !decimal.TryParse(x.LocationId.ToString(), out decimal locationId)) )
                {
                    return BadRequest("Request body is Invalid.");
                }
                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation("[ShopifyUpdateController] Sending VariantLocationUpdate to queue for variant ID: {VariantId}", request.VariantId);
                await _shopifyService.SendUpdateToQueueAsync(jsonRequest, QueueName.VariantLocationUpdate.ToString());
                return Ok("Data added to Queue");
                //var result = await _shopifyUpdateService.UpdateVariantLocationPriceAsync(request);
                //return Ok(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVariantLocationPrice", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }

        }

        /// <summary>
        /// Updates inventory levels for multiple variants across different locations in Shopify.
        /// </summary>
        /// <param name="request">The request containing inventory level update information.</param>
        /// <returns>Status of the update or an error response.</returns>
        [HttpPut("UpdateInventoryLevels")]
        public async Task<IActionResult> UpdateInventoryLevels([FromBody] UpdateInventoryRequest request)
        {
            try
            {
                _logger.LogInformation("[ShopifyUpdateController] UpdateInventoryLevels API called");

                if (request == null || request.InventoryItems == null || !request.InventoryItems.Any())
                {
                    _commonService.ErrorLogs("Invalid request body", "UpdateInventoryLevels", 1, "Request is null or contains no inventory items", "BadRequest");
                    return BadRequest("Request body is invalid or contains no inventory items.");
                }

                // Validate each inventory item
                foreach (var item in request.InventoryItems)
                {
                    if (item.LocationId <= 0 || item.VariantId <= 0 || item.AvailableQuantity < 0)
                    {
                        _commonService.ErrorLogs("Invalid inventory item data", "UpdateInventoryLevels", 1, 
                            $"Invalid data for item: LocationId={item.LocationId}, VariantId={item.VariantId}, AvailableQuantity={item.AvailableQuantity}", "BadRequest");
                        return BadRequest($"Invalid data for inventory item: LocationId={item.LocationId}, VariantId={item.VariantId}, AvailableQuantity={item.AvailableQuantity}");
                    }
                }

                _logger.LogInformation("[ShopifyUpdateController] Processing {Count} inventory items", request.InventoryItems.Count);

                string jsonRequest = JsonConvert.SerializeObject(request);
                _logger.LogInformation("[ShopifyUpdateController] Sending InventoryLevelUpdate to queue for {Count} items", request.InventoryItems.Count);
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