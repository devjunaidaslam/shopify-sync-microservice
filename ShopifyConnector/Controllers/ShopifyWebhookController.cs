using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Enum;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShopifyConnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopifyWebhookController : ControllerBase
    {
        //private readonly string AccessToken;
        //public ShopifyWebhookController(IOptions<ShopifySetting> settings)
        //{
        //    AccessToken = settings.Value.Token;
        //}
        private readonly IWebHookService _webHookService;
        private readonly ICommonService _commonService;
        private readonly IShopifyService _shopifyService;
        private readonly ILogger<ShopifyWebhookController> _logger;
        private readonly string _webHookSecret;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyWebhookController"/> class.
        /// </summary>
        /// <param name="webHookService">The webhook service to process Shopify webhooks.</param>
        /// <param name="commonService">The common service for logging.</param>
        /// <param name="shopifyService">Shopify service that orchestrates RabbitMQ operations.</param>
        /// <param name="setting">Shopify settings including webhook secret.</param>
        /// <param name="logger">Logger for application logging.</param>
        public ShopifyWebhookController(IWebHookService webHookService, ICommonService commonService, IShopifyService shopifyService, IOptions<ShopifySetting> setting, ILogger<ShopifyWebhookController> logger)
        {
            _webHookService = webHookService;
            _commonService = commonService;
            _shopifyService = shopifyService;
            _webHookSecret = setting.Value.WebHookSecret;
            _logger = logger;
        }

        /// <summary>
        /// Handles the Shopify webhook for product creation or update. It send data to RabbitMQ Message Broker
        /// </summary>
        /// <returns>Status of the webhook processing or an error response.</returns>
        [HttpPost("UpsertProduct")]
        public async Task<IActionResult> UpsertProduct()
        {
            var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"].FirstOrDefault();
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
           bool isValid = _webHookService.IsValidWebhook(body, hmacHeader, _webHookSecret);

            if (!isValid)
            {
                return Unauthorized(); // 401
            }

            try
            {
                _logger.LogInformation("[ShopifyWebhookController] Sending ProductWebhook to queue");

                await _shopifyService.SendWebhookToQueueAsync(body, QueueName.ProductWebhook.ToString() );
                return Ok("Product processed successfully.");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertProduct", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Handles the Shopify webhook for collection creation or update.
        /// </summary>
        /// <returns></returns>
        [HttpPost("UpsertCollection")]
        public async Task<IActionResult> UpsertCollectionAsync()
        {
            var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"].FirstOrDefault();
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            bool isValid = _webHookService.IsValidWebhook(body, hmacHeader, _webHookSecret);

            if (!isValid)
            {
                return Unauthorized(); // 401
            }

            try
            {
                // Deserialize the JSON body to JsonElement
                //var jsonBody = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
                //await _webHookService.ProcessCollectionCreatedOrUpdatedAsync(jsonBody);
                _logger.LogInformation("[ShopifyWebhookController] Sending CollectionWebhook to queue");
                await _shopifyService.SendWebhookToQueueAsync(body, QueueName.CollectionWebhook.ToString());
                return Ok("Collection processed successfully.");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertCollectionAsync", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Handles the Shopify webhook for inventory level updates.
        /// </summary>
        /// <returns></returns>
        [HttpPost("UpdateInventoryLevel")]
        public async Task<IActionResult> UpdateInventoryLevel()
        {
            var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"].FirstOrDefault();
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            bool isValid = _webHookService.IsValidWebhook(body, hmacHeader, _webHookSecret);

            if (!isValid)
            {
                return Unauthorized(); // 401
            }

            try
            {
                // Deserialize the JSON body to JsonElement
                //var jsonBody = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
                //await _webHookService.UpdateInventoryLevel(jsonBody);
                _logger.LogInformation("[ShopifyWebhookController] Sending InventoryLevelWebhook to queue");
                await _shopifyService.SendWebhookToQueueAsync(body, QueueName.InventoryLevelWebhook.ToString());
                return Ok("UpdateInventoryLevel processed successfully.");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateInventoryLevel", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Handles the Shopify webhook for order creation or update.
        /// This endpoint receives order data from Shopify and queues it for processing.
        /// </summary>
        /// <returns>Status of the webhook processing or an error response.</returns>
        [HttpPost("UpsertOrder")]
        public async Task<IActionResult> UpsertOrder()
        {
            var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"].FirstOrDefault();
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            bool isValid = _webHookService.IsValidWebhook(body, hmacHeader, _webHookSecret);

            if (!isValid)
            {
                _logger.LogWarning("[ShopifyWebhookController] Invalid HMAC signature for order webhook");
                return Unauthorized(); // 401
            }

            try
            {
                _logger.LogInformation("[ShopifyWebhookController] Sending OrderWebhook to queue");
                await _shopifyService.SendWebhookToQueueAsync(body, QueueName.OrderWebhook.ToString());
                return Ok("Order processed successfully.");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpsertOrder", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[ShopifyWebhookController] Error processing order webhook: {ErrorMessage}", ex.Message);
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        //[HttpGet]
        //public  IActionResult HealthCheck()
        //{
        //    return Ok("Health Check Successfully");
        //}

    }
}