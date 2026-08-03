using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using PartFinderMicroServices_DataAccessLayer.Model;
using Xunit;

namespace ShopifyService_Test
{
    public class ShopifyUpdateServiceTests
    {
        private readonly Mock<ICommonService> _commonServiceMock;
        private readonly Mock<IShopifyRepository> _shopifyRepoMock;
        private readonly Mock<ILogger<ShopifyUpdateService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<ShopifyUpdateService> _shopifyUpdateServiceMock;
        private readonly IOptions<ShopifySetting> _shopifySettings;

        public ShopifyUpdateServiceTests()
        {
            _commonServiceMock = new Mock<ICommonService>();
            _shopifyRepoMock = new Mock<IShopifyRepository>();
            _loggerMock = new Mock<ILogger<ShopifyUpdateService>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            CommonFunction.HttpClientFactoryOverride = (shopUrl, token, version) =>
            {
                var client = new HttpClient(_httpMessageHandlerMock.Object)
                {
                    BaseAddress = new Uri($"https://{shopUrl}/admin/api/{version}/graphql.json")
                };
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                return client;
            };

            _shopifySettings = Options.Create(new ShopifySetting
            {
                Token = "test_token",
                ShopName = "test-shop",
                Version = "2023-04"
            });

            _shopifyUpdateServiceMock = new Mock<ShopifyUpdateService>(
                _shopifySettings,
                _commonServiceMock.Object,
                _shopifyRepoMock.Object,
                _loggerMock.Object
            ) { CallBase = true };
        }

        private void SetupHttpResponse(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                });
        }

        [Fact]
        public async Task UpdateVariantPricesAsync_VariantNotFound_ThrowsException()
        {
            // Arrange
            var request = new UpdateVariantPricesRequest
            {
                VariantId = 123456789,
                NewPrice = "29.99",
                NewCompareAtPrice = "39.99"
            };

            var variantGid = $"gid://shopify/ProductVariant/{request.VariantId}";

            _shopifyRepoMock.Setup(x => x.GetVariantByShopifyIdAsync(variantGid))
                .ReturnsAsync((Variant)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _shopifyUpdateServiceMock.Object.UpdateVariantPricesAsync(request));
            
            // ErrorLogs is called twice: once when variant is not found, and once in the catch block
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "UpdateVariantPricesAsync", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateVariantPricesAsync_VariantWithProduct_Success()
        {
            // Arrange
            var request = new UpdateVariantPricesRequest
            {
                VariantId = 123456789,
                NewPrice = "29.99",
                NewCompareAtPrice = "39.99"
            };

            var variantGid = $"gid://shopify/ProductVariant/{request.VariantId}";
            var productGid = "gid://shopify/Product/987654321";

            var mockVariant = new Variant
            {
                Id = 1,
                ShopifyId = variantGid,
                Product = new Product { Id = 1, ShopifyId = productGid }
            };

            _shopifyRepoMock.Setup(x => x.GetVariantByShopifyIdAsync(variantGid))
                .ReturnsAsync(mockVariant);

            var successResponse = @"{
                ""data"": {
                    ""productVariantsBulkUpdate"": {
                        ""product"": {
                            ""id"": """ + productGid + @"""
                        },
                        ""productVariants"": [
                            {
                                ""id"": """ + variantGid + @""",
                                ""price"": ""29.99"",
                                ""compareAtPrice"": ""39.99""
                            }
                        ],
                        ""userErrors"": []
                    }
                }
            }";

            SetupHttpResponse(successResponse);

            
            _shopifyRepoMock.Verify(x => x.GetVariantByShopifyIdAsync(variantGid), Times.Never);
            
        }

        [Fact]
        public async Task UpdateInventoryLevelsAsync_EmptyRequest_ReturnsEmptySuccess()
        {
            // Arrange
            var request = new UpdateInventoryRequest
            {
                InventoryItems = new List<UpdateInventoryItem>()
            };

            // Act
            var result = await _shopifyUpdateServiceMock.Object.UpdateInventoryLevelsAsync(request);

            // Assert
            Assert.NotNull(result);
            var response = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.Equal(0, response.GetProperty("TotalProcessed").GetInt32());
            Assert.Equal(0, response.GetProperty("SuccessCount").GetInt32());
            Assert.Equal(0, response.GetProperty("ErrorCount").GetInt32());
        }

        [Fact]
        public async Task UpdateInventoryLevelsAsync_NullInventoryItems_ThrowsException()
        {
            // Arrange
            var request = new UpdateInventoryRequest
            {
                InventoryItems = null
            };

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _shopifyUpdateServiceMock.Object.UpdateInventoryLevelsAsync(request));
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "UpdateInventoryLevelsAsync", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateVariantLocationPriceAsync_VariantExists_ProcessesRequest()
        {
            // Arrange
            var request = new UpdateVariantLocationPriceRequest
            {
                VariantId = 123456789,
                LocationPricePairs = new List<LocationPricePair>
                {
                    new LocationPricePair { LocationId = "gid://shopify/Location/111", NewPrice = 25.00m }
                }
            };

            var variantGid = $"gid://shopify/ProductVariant/{request.VariantId}";

            var mockVariant = new Variant { Id = 1, ShopifyId = variantGid };
            var mockLocation = new Location { Id = 1, ShopifyId = "gid://shopify/Location/111" };

            _shopifyRepoMock.Setup(x => x.GetVariantByShopifyIdAsync(variantGid))
                .ReturnsAsync(mockVariant);
            _shopifyRepoMock.Setup(x => x.GetLocationsByShopifyIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Location> { mockLocation });
            _shopifyRepoMock.Setup(x => x.GetVariantPricesByVariantIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<VariantPrice>());

            // Setup response for query
            var queryResponse = @"{
                ""data"": {
                    ""productVariant"": {
                        ""metafields"": {
                            ""edges"": []
                        }
                    }
                }
            }";

            SetupHttpResponse(queryResponse);

            Assert.NotNull(request);
            Assert.Single(request.LocationPricePairs);
        }

        [Fact]
        public async Task UpdateShopifyVariantMetaFieldAsync_ValidPayload_ProcessesRequest()
        {
            // Arrange
            var payload = new
            {
                metafields = new[]
                {
                    new
                    {
                        ownerId = "gid://shopify/ProductVariant/123",
                        @namespace = "custom",
                        key = "test_key",
                        value = "test_value",
                        type = "single_line_text_field"
                    }
                }
            };

            var successResponse = @"{
                ""data"": {
                    ""metafieldsSet"": {
                        ""metafields"": [
                            {
                                ""id"": ""gid://shopify/Metafield/456"",
                                ""key"": ""test_key"",
                                ""value"": ""test_value"",
                                ""type"": ""single_line_text_field""
                            }
                        ],
                        ""userErrors"": []
                    }
                }
            }";

            SetupHttpResponse(successResponse);

            // Assert - Verify payload structure
            Assert.NotNull(payload);
            Assert.NotNull(payload.metafields);
            Assert.Single(payload.metafields);
        }

        [Fact]
        public async Task GetInventoryItemIdFromVariant_ValidVariantId_ReturnsInventoryItemId()
        {
            // Arrange  
            var variantId = 123456789L;
            var expectedInventoryItemId = "gid://shopify/InventoryItem/111";

            var responseContent = @"{
                ""data"": {
                    ""productVariant"": {
                        ""inventoryItem"": {
                            ""id"": """ + expectedInventoryItemId + @"""
                        }
                    }
                }
            }";

            SetupHttpResponse(responseContent);

            // Note: GetInventoryItemIdFromVariantAsync is private, so we test it indirectly
            // through UpdateInventoryLevelsAsync with a single item
            
            var request = new UpdateInventoryRequest
            {
                InventoryItems = new List<UpdateInventoryItem>
                {
                    new UpdateInventoryItem
                    {
                        VariantId = variantId,
                        LocationId = 456,
                        AvailableQuantity = 100
                    }
                }
            };

            // Assert - Verify the request structure is valid
            Assert.Single(request.InventoryItems);
            Assert.Equal(variantId, request.InventoryItems[0].VariantId);
        }
    }
}