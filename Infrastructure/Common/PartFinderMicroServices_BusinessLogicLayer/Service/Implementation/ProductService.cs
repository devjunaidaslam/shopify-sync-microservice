using Microsoft.Extensions.Logging;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service for handling product-related business logic.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICommonService _commonService;
        private readonly ILogger<ProductService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService"/> class.
        /// </summary>
        /// <param name="productRepository">Repository for product data access.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        /// <param name="logger">Logger for application logging.</param>
        public ProductService(IProductRepository productRepository, ICommonService commonService, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _commonService = commonService;
            _logger = logger;
            
            _logger.LogInformation("[ProductService] Service initialized successfully");
        }

        /// <summary>
        /// Retrieves a paginated list of products based on the provided filter.
        /// </summary>
        /// <param name="productFilterDto">The filter criteria for products, including search, page number, and page size.</param>
        /// <returns>A Response object containing the paginated list of products and pagination info.</returns>
        public async Task<Response> GetAllProductsPaginatedAsync(ProductFilterDto productFilterDto)
        {
            try
            {
                _logger.LogInformation("[ProductService] Attempting to get all products paginated with filter: {ProductFilterDto}", productFilterDto);
                var data = await _productRepository.GetAllProductsPaginatedAsync(productFilterDto);
                var totalItemCount = data.TotalCount;
                int totalPages = productFilterDto.page_size > 0 ? (int)Math.Ceiling((double)totalItemCount / productFilterDto.page_size) : 0;
                var pagination = new PaginationInfo
                {
                    TotalItemCount = totalItemCount,
                    PageNo = productFilterDto.page_no,
                    PerPage = productFilterDto.page_size,
                    TotalPages = totalPages,
                    NextPage = productFilterDto.page_no < totalPages ? productFilterDto.page_no + 1 : 0,
                    PrevPage = productFilterDto.page_no > 1 ? productFilterDto.page_no - 1 : 0
                };
                //var userRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                _logger.LogInformation("[ProductService] Successfully fetched {TotalItemCount} products for page {PageNo} with {TotalPages} pages", totalItemCount, productFilterDto.page_no, totalPages);
                return ResponseHelper.Success("Products fetched successfully", data.Data, null, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllProductsPaginatedAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[ProductService] Error fetching all products paginated: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a product by its unique identifier.
        /// </summary>
        /// <param name="productId">The unique identifier of the product.</param>
        /// <returns>A Response object containing the product details or an error message.</returns>
        public async Task<Response> GetProductAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return ResponseHelper.BadRequest("Product id is not valid");
                }
                _logger.LogInformation("[ProductService] Attempting to get product with ID: {ProductId}", productId);
                var data = await _productRepository.GetProductByIdAsync(productId);
                //var userRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                if (data == null)
                {
                    _logger.LogWarning("[ProductService] Product with ID {ProductId} not found", productId);
                    return ResponseHelper.NotFound("Product not found");
                }
                _logger.LogInformation("[ProductService] Successfully fetched product with ID: {ProductId}", productId);
                return ResponseHelper.Success("Product fetched successfully", data);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetProductAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[ProductService] Error fetching product with ID {ProductId}: {ErrorMessage}", productId, ex.Message);
                throw;
            }
        }
    }
}
