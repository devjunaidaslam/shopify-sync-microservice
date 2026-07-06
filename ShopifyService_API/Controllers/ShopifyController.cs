using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure.Job.Background;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopifyController : ControllerBase
    {
        private readonly ImportProductsBackgroundService _importProductsBackgroundService;
        private readonly IShopifyService _shopifyService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopifyController"/> class.
        /// </summary>
        /// <param name="importProductsBackgroundService">Background service for importing products from Shopify.</param>
        /// <param name="shopifyService">Service for Shopify operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public ShopifyController(ImportProductsBackgroundService importProductsBackgroundService, IShopifyService shopifyService, ICommonService commonService)
        {
            _importProductsBackgroundService = importProductsBackgroundService;
            _shopifyService = shopifyService;
            _commonService = commonService;
        }

        /// <summary>
        /// Triggers the background job to import products from Shopify, optionally for a specific date.
        /// </summary>
        /// <param name="date">Optional date to filter products to import.</param>
        /// <returns>Status of the import trigger or an error response.</returns>
        [HttpPost("TriggerImportProducts")]
        public async Task<IActionResult> TriggerImportProducts(DateTime? date = null)
        {
            try
            {
                if (_importProductsBackgroundService.IsRunning)
                {
                    return Conflict("ImportProducts job is already running.");
                }
                _importProductsBackgroundService.TriggerImport(date);
                return Ok("ImportProducts background job triggered.");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "TriggerImportProducts", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Imports a product from Shopify by its unique identifier.
        /// </summary>
        /// <param name="productId">The unique identifier of the product in Shopify.</param>
        /// <returns>Status of the import or an error response.</returns>
        [HttpPost("ImportProductById/{productId}")]
        public async Task<IActionResult> ImportProductByIdAsync(long productId)
        {
            try 
            {
               
                if ( productId <= 0)
                {
                    return BadRequest("Request body is Invalid.");
                }
                var productImportStatus = await _shopifyService.ImportProductByIdAsync(productId);
                if (productImportStatus == null)
                {
                    return NotFound($"Product with ID {productId} does not exist in Shopify.");
                }
                if (productImportStatus.Contains("401"))
                {
                    return Unauthorized(productImportStatus);
                }
                return Ok(productImportStatus);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportProductByIdAsync", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
