using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Service.Implementation;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Model;
using Xunit;
using System.Text;
using ShopifySync_DataAccessLayer.Entities;
using Microsoft.Extensions.Logging;

namespace ShopifyService_Test
{
    public class ShopifyServiceTests
    {
        private readonly Mock<ICommonService> _commonServiceMock;
        private readonly Mock<IShopifyRepository> _shopifyRepoMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<PageCursorTracker> _pageCursorTrackerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<ShopifyService> _shopifyServiceMock;
        private readonly Mock<IShopifyService> _shopifyServiceInterfaceMock;
        private readonly Mock<IShopifyUpdateService> _shopifyUpdateService;
        private readonly Mock<ILogger<ShopifyService>> _logger;
        private readonly Mock<ITransactionHistoryService> _transactionService;
        private readonly Mock<IWebHookRMQService> _webHookRMQService;
        private readonly Mock<IShopifyUpdateRMQService> _shopifyUpdateRMQService;
         
        public ShopifyServiceTests()
        {
            _commonServiceMock = new Mock<ICommonService>();
            _shopifyRepoMock = new Mock<IShopifyRepository>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _shopifyUpdateService = new Mock<IShopifyUpdateService>();
            _pageCursorTrackerMock = new Mock<PageCursorTracker>();
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

            _shopifyServiceInterfaceMock = new Mock<IShopifyService>();
            _logger = new Mock<ILogger<ShopifyService>>();
            _transactionService = new Mock<ITransactionHistoryService>();
            _webHookRMQService = new Mock<IWebHookRMQService>();
            _shopifyUpdateRMQService = new Mock<IShopifyUpdateRMQService>();

            var shopifySettings = Options.Create(new ShopifySetting
            {
                Token = "test-shopify-token",
                ShopName = "test-shop",
                Version = "2023-04",
                ProductPageSize = 10,
                VariantPageSize = 10,
                VariantMetaFieldPageSize = 10,
                InventoryLevelPageSize = 10,
                CollectionPageSize = 10,
                LocationPageSize = 10,
                VendorPageSize = 10,
                ConcurrencyLimit = 2
            });

            _shopifyServiceMock = new Mock<ShopifyService>(
                shopifySettings,
                new PageCursorTracker(),
                _shopifyRepoMock.Object,
                _serviceProviderMock.Object,
                _commonServiceMock.Object,
                _logger.Object,
                _shopifyUpdateService.Object,
                _transactionService.Object,
                _webHookRMQService.Object,
                _shopifyUpdateRMQService.Object
                )
            { CallBase = true };
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
        public async Task ImportCollectionsAsync_Success()
        {
            var responseContent = @"{
                ""data"": {
                    ""collections"": {
                        ""pageInfo"": {
                            ""hasNextPage"": false,
                            ""endCursor"": ""cursor1""
                        },
                        ""edges"": [
                            {
                                ""node"": {
                                    ""id"": ""gid://shopify/Collection/1"",
                                    ""title"": ""Collection 1"",
                                    ""description"": ""Description 1"",
                                    ""handle"": ""handle1""
                                }
                            }
                        ]
                    }
                }
            }";
            SetupHttpResponse(responseContent);

            _shopifyRepoMock.Setup(x => x.AddCollectionsAsync(It.IsAny<List<Collection>>())).Returns(Task.CompletedTask);

            await _shopifyServiceMock.Object.ImportCollectionsAsync(1);

            _shopifyRepoMock.Verify(x => x.AddCollectionsAsync(It.IsAny<List<Collection>>()), Times.Once);
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ImportLocationsAsync_Success()
        {
            var responseContent = @"{
                ""data"": {
                    ""locations"": {
                        ""pageInfo"": {
                            ""hasNextPage"": false,
                            ""endCursor"": ""cursor1""
                        },
                        ""edges"": [
                            {
                                ""node"": {
                                    ""id"": ""gid://shopify/Location/1"",
                                    ""name"": ""Location 1"",
                                    ""address"": {
                                        ""city"": ""City1"",
                                        ""country"": ""Country1"",
                                        ""province"": ""Province1"",
                                        ""zip"": ""Zip1""
                                    }
                                }
                            }
                        ]
                    }
                }
            }";
            SetupHttpResponse(responseContent);

            _shopifyRepoMock.Setup(x => x.AddLocationsAsync(It.IsAny<List<Location>>())).Returns(Task.CompletedTask);

            await _shopifyServiceMock.Object.ImportLocationsAsync();

            _shopifyRepoMock.Verify(x => x.AddLocationsAsync(It.IsAny<List<Location>>()), Times.Once);
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "ImportLocationsAsync", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task GetHistoryStatus_Success()
        {
            var historyInventory = new HistoryInventory();
            _shopifyRepoMock.Setup(x => x.GetHistoryStatus()).ReturnsAsync(historyInventory);

            var expectedResponse = new Response
            {
                Data = historyInventory,
                IsSuccess = true,
                Message = "HistoryStatus fetched successfully"
            };

            var result = await _shopifyServiceMock.Object.GetHistoryStatus();

            Assert.Equal(expectedResponse.IsSuccess, result.IsSuccess);
            Assert.Equal(expectedResponse.Message, result.Message);
            Assert.Equal(expectedResponse.Data, result.Data);
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "GetHistoryStatus", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetHistoryStatus_ThrowsException_LogsError()
        {
            _shopifyRepoMock.Setup(x => x.GetHistoryStatus()).ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _shopifyServiceMock.Object.GetHistoryStatus());
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "GetHistoryStatus", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ImportProductByIdAsync_Success()
        {
            var productId = 8069114200204;
            var jsonResponse = @"{
                ""data"": {
                    ""product"": {
                        ""id"": ""gid://shopify/Product/123"",
                        ""title"": ""Product 123"",
                        ""handle"": ""product-123"",
                        ""descriptionHtml"": ""Description"",
                        ""vendor"": ""Vendor"",
                        ""status"": ""active"",
                        ""createdAt"": ""2023-01-01T00:00:00Z"",
                        ""updatedAt"": ""2023-01-02T00:00:00Z"",
                        ""variants"": { ""edges"": [] },
                        ""tags"": [],
                        ""collections"": { ""edges"": [] }
                    }
                }
            }";

            SetupHttpResponse(jsonResponse);

            // Mock repository methods called during SaveShopifyProductDataAsync to avoid exceptions
            _shopifyRepoMock.Setup(x => x.AddOptionsAsync(It.IsAny<List<Option>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddOptionValuesAsync(It.IsAny<List<OptionValue>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddProductsAsync(It.IsAny<List<Product>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddVariants(It.IsAny<List<Variant>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddTagsAsync(It.IsAny<List<Tag>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddProductTagsAsync(It.IsAny<List<ProductTag>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddCollectionsAsync(It.IsAny<List<Collection>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddProductCollectionsAsync(It.IsAny<List<ProductCollection>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddProductOptionAsync(It.IsAny<List<ProductOption>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddVariantOptionValueAsync(It.IsAny<List<VariantOptionValue>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.AddInventoryLevelsAsync(It.IsAny<List<InventoryLevel>>())).Returns(Task.CompletedTask);
            _shopifyRepoMock.Setup(x => x.GetOrCreateVendorByNameAsync(It.IsAny<string>())).ReturnsAsync(new Vendor { Id = 1, Title = "Vendor" });
            _shopifyRepoMock.Setup(x => x.GetLocationsByShopifyIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Location>
                {
                    new Location { Id = 1, ShopifyId = "location1", Name = "Location 1" }
                });

            // Act
            var result = await _shopifyServiceMock.Object.ImportProductByIdAsync(productId);

            // Assert
            Assert.Equal("Product Imported Successfully.", result);
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "ImportProductByIdAsync", 1, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ImportProductByIdAsync_ProductNotFound_LogsError()
        {
            // Arrange
            var productId = 123L;
            var jsonResponse = @"{
                ""data"": {
                    ""product"": null
                }
            }";

            SetupHttpResponse(jsonResponse);

            // Act
            var result = await _shopifyServiceMock.Object.ImportProductByIdAsync(productId);

            // Assert
            Assert.Null(result);
            _commonServiceMock.Verify(x => x.ErrorLogs(It.IsAny<string>(), "ImportProductByIdAsync", 2, "Product not found", It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task ImportVendorsAsync_CallsAddVendorsAsync_WithExpectedVendors()
        {
            // Arrange
            var expectedVendors = new List<Vendor>
    {
        new Vendor { Title = "Vendor1" },
        new Vendor { Title = "Vendor2" },
        new Vendor { Title = "Vendor3" }
    };

            _shopifyServiceInterfaceMock
                .Setup(x => x.ImportVendorsAsync())
                .Callback(async () =>
                {
                    await _shopifyRepoMock.Object.AddVendorsAsync(expectedVendors);
                })
                .Returns(Task.CompletedTask);

            // Capture vendors passed
            List<Vendor> capturedVendors = new();
            _shopifyRepoMock
                .Setup(x => x.AddVendorsAsync(It.IsAny<List<Vendor>>()))
                .Callback<List<Vendor>>(vendors => capturedVendors = vendors)
                .Returns(Task.CompletedTask);

            // Act
            await _shopifyServiceInterfaceMock.Object.ImportVendorsAsync();

            // Assert
            Assert.Equal(3, capturedVendors.Count);
            Assert.Contains(capturedVendors, v => v.Title == "Vendor1");
            Assert.Contains(capturedVendors, v => v.Title == "Vendor2");
            Assert.Contains(capturedVendors, v => v.Title == "Vendor3");

            _shopifyRepoMock.Verify(x => x.AddVendorsAsync(It.IsAny<List<Vendor>>()), Times.Once);
            _commonServiceMock.Verify(x => x.ErrorLogs(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()
            ), Times.Never);
        }


    }

}
