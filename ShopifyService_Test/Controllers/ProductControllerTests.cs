using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using ShopifyService_API.Controllers;
using Xunit;

namespace PartService_Test.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly ProductController _productControllerMock;
        private readonly Mock<ICommonService> _commonServiceMock;

        public ProductControllerTests()
        {
            _commonServiceMock = new Mock<ICommonService>();
            _productServiceMock = new Mock<IProductService>();
            _productControllerMock = new ProductController(_productServiceMock.Object , _commonServiceMock.Object);
        }

        [Fact]
        public async Task GetAllProductsPaginatedAsync_ShouldReturnPaginatedProducts()
        {
            // Arrange
            var filter = new ProductFilterDto
            {
                page_no = 1,
                page_size = 20,
                search = "Test"
            };
            _productServiceMock.Setup(service => service.GetAllProductsPaginatedAsync(It.Is<ProductFilterDto>(d => d.page_no == filter.page_no && d.page_size == filter.page_size && d.search == filter.search)))
                .ReturnsAsync(new Response
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Data = new List<Product> { new() { Id = 1, Title = "Test Product" } },
                    Message = "Products fetched successfully"
                });
            // Act
            var result = await _productControllerMock.GetAllProducts(filter);
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Value!.IsSuccess);
            Assert.Equal(200, result.Value.StatusCode);
            Assert.NotNull(result.Value.Data);
            var products = Assert.IsAssignableFrom<List<Product>>(result.Value.Data);
            Assert.Single(products);
            Assert.Equal(1, products[0].Id);
            Assert.Equal("Test Product", products[0].Title);
            Assert.Equal("Products fetched successfully", result.Value.Message);
        }

        [Fact]
        public async Task GetAllProductsPaginatedAsync_ShouldReturnEmptyList()
        {
            // Arrange
            var filter = new ProductFilterDto
            {
                page_no = 1,
                page_size = 20,
                search = string.Empty
            };
            _productServiceMock.Setup(service => service.GetAllProductsPaginatedAsync(It.IsAny<ProductFilterDto>()))
                .ReturnsAsync(new Response
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Data = new List<Product>()
                });
            // Act
            var result = await _productControllerMock.GetAllProducts(filter);
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Value!.IsSuccess);
            Assert.Equal(200, result.Value.StatusCode);
            var products = Assert.IsAssignableFrom<List<Product>>(result.Value.Data);
            Assert.Empty(products);
        }

        [Fact]
        public async Task GetAllProductsPaginatedAsync_ShouldReturnFailure()
        {
            // Arrange
            var filter = new ProductFilterDto
            {
                page_no = 1,
                page_size = 20,
                search = string.Empty
            };
            _productServiceMock.Setup(service => service.GetAllProductsPaginatedAsync(It.IsAny<ProductFilterDto>()))
                .ReturnsAsync(new Response
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Data = null,
                    Message = "Internal Server Error"
                });
            // Act
            var result = await _productControllerMock.GetAllProducts(filter);
            // Assert
            Assert.NotNull(result);
            Assert.False(result.Value!.IsSuccess);
            Assert.Equal(500, result.Value.StatusCode);
            Assert.Null(result.Value.Data);
            Assert.Equal("Internal Server Error", result.Value.Message);
        }

        [Fact]
        public async Task GetAllProductsPaginatedAsync_ShouldReturnNullResponse()
        {
            // Arrange
            var filter = new ProductFilterDto
            {
                page_no = 1,
                page_size = 20,
                search = string.Empty
            };
            _productServiceMock.Setup(service => service.GetAllProductsPaginatedAsync(It.IsAny<ProductFilterDto>()))
                .ReturnsAsync((Response)null!);
            // Act
            var result = await _productControllerMock.GetAllProducts(filter);
            // Assert
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task GetProduct_ShouldReturnProduct()
        {
            // Arrange
            _productServiceMock.Setup(service => service.GetProductAsync(It.IsAny<int>()))
                .ReturnsAsync(new Response
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = new Product { Id = 1, Title = "Test Product" },
                    Message = "Product fetched successfully"
                });

            // Act
            var actionResult = await _productControllerMock.GetProduct(1);

            // Assert
            Assert.NotNull(actionResult);
            var result = actionResult.Result;
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okObjectResult.StatusCode);
            // The controller returns response.Data directly, not the entire Response object
            var product = Assert.IsType<Product>(okObjectResult.Value);
            Assert.NotNull(product);
            Assert.Equal(1, product.Id);
            Assert.Equal("Test Product", product.Title);
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ShouldReturnBadRequest()
        {
            // Arrange
            _productServiceMock.Setup(service => service.GetProductAsync(It.IsAny<int>()))
                .ReturnsAsync(new Response
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Data = null,
                    Message = "Product id is not valid"
                });

            // Act
            var actionResult = await _productControllerMock.GetProduct(-1);

            // Assert
            Assert.NotNull(actionResult);
            var result = actionResult.Result;
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
            var response = Assert.IsType<Response>(badRequest.Value);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.Null(response.Data);
            Assert.Equal("Product id is not valid", response.Message);
        }

        [Fact]
        public async Task GetProduct_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            _productServiceMock.Setup(service => service.GetProductAsync(It.IsAny<int>()))
                .ReturnsAsync(new Response
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Data = null,
                    Message = "Product not found"
                });
            // Act
            var actionResult = await _productControllerMock.GetProduct(999);

            // Assert
            Assert.NotNull(actionResult);
            var result = actionResult.Result;
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            var response = Assert.IsType<Response>(notFound.Value);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Null(response.Data);
            Assert.Equal("Product not found", response.Message);
        }
    }
}