using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryInventoryController : ControllerBase
    {
        private readonly IShopifyService _shopifyService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryInventoryController"/> class.
        /// </summary>
        /// <param name="shopifyService">The Shopify service to retrieve history inventory status.</param>
        /// <param name="commonService">The common service for logging.</param>
        public HistoryInventoryController(IShopifyService shopifyService, ICommonService commonService)
        {
            _shopifyService = shopifyService;
            _commonService = commonService;
        }

        /// <summary>
        /// Gets the status of the history inventory from Shopify.
        /// </summary>
        /// <returns>An IActionResult containing the history status or an error message.</returns>
        [HttpGet("GetHistoryStatus")]
        public async Task<ActionResult<Response>> GetHistoryStatus()
        {
            try
            {
                var history = await _shopifyService.GetHistoryStatus();
                return Ok(history);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetHistoryStatus", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Gets paginated history status records with filtering support.
        /// </summary>
        /// <param name="filterDto">Filter and pagination options for history records. Includes Search, Page_No (page number), Page_Size (items per page), FromDate, ToDate, InProgress, and IsSuccess filters.</param>
        /// <returns>A paginated list of history status records or an error response.</returns>
        [HttpGet("GetHistoryStatusPaginated")]
        public async Task<ActionResult<Response>> GetHistoryStatusPaginated([FromQuery] HistoryInventoryFilterDto filterDto)
        {
            try
            {
                var history = await _shopifyService.GetHistoryStatusPaginated(filterDto);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetHistoryStatusPaginated", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}