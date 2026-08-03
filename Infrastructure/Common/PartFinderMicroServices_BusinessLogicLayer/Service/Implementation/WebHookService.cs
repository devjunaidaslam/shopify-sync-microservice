using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.NewFolder;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OptionSetDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TagSetDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class WebHookService : IWebHookService
    {
        private readonly IShopifyRepository _shopifyRepo;
        private readonly IShopifyService _shopifyService;
        private readonly ICommonService _commonService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebHookService> _logger;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly IOrderService _orderService;
        private readonly string _token;
        private readonly string _shopUrl;
        private readonly string _version;

        public WebHookService(
            IOptions<ShopifySetting> settings, 
            IShopifyRepository shopifyRepo, 
            IShopifyService shopifyService, 
            ICommonService commonService, 
            IConfiguration configuration, 
            ILogger<WebHookService> logger, 
            ITransactionHistoryService transactionHistoryService,
            IOrderService orderService)
        {
            _shopifyRepo = shopifyRepo;
            _shopifyService = shopifyService;
            _commonService = commonService;
            _configuration = configuration;
            _logger = logger;
            _transactionHistoryService = transactionHistoryService;
            _orderService = orderService;
            _token = settings.Value.Token;
            _shopUrl = $"{settings.Value.ShopName}.myshopify.com";
            _version = settings.Value.Version;
            _logger.LogInformation("[WebHookService] Service initialized successfully");
        }

        public async Task ProcessCollectionCreatedOrUpdatedAsync(JsonElement jsonBody)
        {
            var shopifyCollectionId = jsonBody.GetProperty("admin_graphql_api_id").GetString();
            _logger.LogInformation("[WebHookService] ProcessCollectionCreatedOrUpdatedAsync called for collection {ShopifyId}", shopifyCollectionId);
            try
            {

                var existingCollection = await _shopifyRepo.GetCollectionByShopifyId(shopifyCollectionId);

                if (existingCollection == null)
                {
                    _logger.LogInformation("[WebHookService] Collection {ShopifyId} not found, creating new collection", shopifyCollectionId);
                    var collection = await SetCollectionAsync(jsonBody);
                    await _shopifyRepo.AddCollectionsAsync(new List<Collection> { collection });
                    _logger.LogInformation("[WebHookService] Successfully created new collection {ShopifyId}", shopifyCollectionId);
                }
                else
                {
                    _logger.LogInformation("[WebHookService] Collection {ShopifyId} found, updating existing collection", shopifyCollectionId);
                    await UpdateCollectionAsync(existingCollection, jsonBody);
                    await _shopifyRepo.UpdateCollectionAsync(existingCollection);
                }
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ProcessCollectionCreatedAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        private async Task<Collection> SetCollectionAsync(JsonElement root)
        {
            var collection = new Collection
            {
                ShopifyId = root.GetProperty("admin_graphql_api_id").GetString(),
                Title = root.GetProperty("title").GetString(),
                Title_en = null,
                Description = root.TryGetProperty("body_html", out var descNode) ? descNode.GetString() : null,
                Image = root.TryGetProperty("image", out var imgNode) && imgNode.TryGetProperty("src", out var srcNode)
                 ? srcNode.GetString()
                 : null,
                CreatedAt = DateTime.SpecifyKind(DateTime.Parse(root.GetProperty("published_at").GetString()), DateTimeKind.Utc),
                ProductCollections = new List<ProductCollection>()
            };

            return collection;
        }

        private async Task UpdateCollectionAsync(Collection existingCollection, JsonElement root)
        {
            try
            {
                existingCollection.Title = root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind != JsonValueKind.Null ? titleProp.GetString() : existingCollection.Title;
                existingCollection.Description = root.TryGetProperty("body_html", out var descNode) && descNode.ValueKind != JsonValueKind.Null ? descNode.GetString() : existingCollection.Description;
                existingCollection.Image = root.TryGetProperty("image", out var imgNode) && imgNode.TryGetProperty("src", out var srcNode) && srcNode.ValueKind != JsonValueKind.Null
                    ? srcNode.GetString()
                    : existingCollection.Image;

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateCollectionAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }

        public async Task UpdateInventoryLevel(JsonElement jsonBody)
        {
            try
            {
                var client = CommonFunction.ConfigureShopifyHttpClient(_shopUrl, _token, _version);

                var inventoryItemId = jsonBody.GetProperty("inventory_item_id");
                var locationId = jsonBody.GetProperty("location_id");
                var available = jsonBody.GetProperty("available");
                var shopifyInventoryItemId = "gid://shopify/InventoryItem/" + inventoryItemId;

                string query = $@"
                                    {{
                                      inventoryItem(id: ""{shopifyInventoryItemId}"") {{
                                        variant {{
                                          id
                                          title
                                          product {{
                                            id
                                            title
                                            vendor
                                          }}
                                        }}
                                      }}
                                    }}";
                var requestBody = new
                {
                    query = query
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WebHookService] GraphQL call failed");
                    throw new Exception($"GraphQL call failed: {response.StatusCode}");
                }
                _logger.LogInformation("[WebHookService] GraphQL API call has been Successfull");

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);

                var variantId = doc
                    .RootElement
                    .GetProperty("data")
                    .GetProperty("inventoryItem")
                    .GetProperty("variant")
                    .GetProperty("id")
                    .GetString();

                var location = await _shopifyRepo.GetLocationsByShopifyIdAsync(locationId.ToString());

                var variant = await _shopifyRepo.GetVariantByShopifyIdAsync(variantId);

                if (location != null && variant != null)
                {
                    await _shopifyRepo.UpdateInventoryLevelAvailable(location.Id, variant.Id, Convert.ToInt32(available.ToString()));
                    _logger.LogInformation("[WebHookService] Available Updated into Database");
                }
                else
                {
                    _logger.LogInformation($"[WebHookService] location or variant not found in database {0}", json);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Processes the Shopify Order Created/Updated webhook event.
        /// Imports the order from Shopify using the OrderService.
        /// </summary>
        /// <param name="jsonBody">The webhook payload containing order data</param>
        /// <returns></returns>
        public async Task ProcessOrderCreatedOrUpdatedAsync(JsonElement jsonBody)
        {
            try
            {
                long shopifyOrderId = 0;
                if (jsonBody.TryGetProperty("id", out JsonElement idElement))
                {
                    shopifyOrderId = idElement.GetInt64();
                }
                else
                {
                    _logger.LogWarning("[WebHookService] Order webhook received without 'id' field");
                    throw new ArgumentException("Order webhook payload missing 'id' field");
                }

                _logger.LogInformation("[WebHookService] ProcessOrderCreatedOrUpdatedAsync called for order ID: {ShopifyOrderId}", shopifyOrderId);

                var importRequest = new ShopifyOrderImportRequestDTO
                {
                    ShopifyOrderId = shopifyOrderId,
                    SupplierLocationNames = new HashSet<string> { "FOURNISSEUR" } // Default supplier names
                };

                var result = await _orderService.ImportOrderFromShopifyAsync(importRequest);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("[WebHookService] Successfully processed order {ShopifyOrderId}", shopifyOrderId);
                }
                else
                {
                    _logger.LogWarning("[WebHookService] Failed to process order {ShopifyOrderId}: {Message}", shopifyOrderId, result.Message);
                    throw new Exception($"Order import failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookService] Error processing order webhook: {Message}", ex.Message);
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ProcessOrderCreatedOrUpdatedAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }
       
        public bool IsValidWebhook(string requestBody, string shopifyHmacHeader, string webhookSecret)
        {
            if (string.IsNullOrEmpty(shopifyHmacHeader))
            {
                return false;
            }

            var secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(requestBody);

            using var hmac = new HMACSHA256(secretBytes);
            var hashBytes = hmac.ComputeHash(bodyBytes);

            byte[] headerBytes;
            try
            {
                headerBytes = Convert.FromBase64String(shopifyHmacHeader);
            }
            catch (FormatException)
            {
                return false;
            }

            return hashBytes.Length == headerBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(hashBytes, headerBytes);
        }

    }
}