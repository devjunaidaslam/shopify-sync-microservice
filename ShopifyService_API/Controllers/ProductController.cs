using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public ProductController(IProductService productService, ICommonService commonService)
        {
            _productService = productService;
            _commonService = commonService;
        }

        [HttpGet]
        public async Task<ActionResult<Response>> GetAllProducts([FromQuery] ProductFilterDto productFilterDto)
        {
            try
            {
                return await _productService.GetAllProductsPaginatedAsync(productFilterDto);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllProducts", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<Response>> GetProduct(int productId)
        {
            try
            {
                var response = await _productService.GetProductAsync(productId);
                if (response == null || !response.IsSuccess)
                {
                    if (response?.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                    if (response?.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                return Ok(response?.Data);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetProduct", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
