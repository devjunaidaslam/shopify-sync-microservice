using Microsoft.AspNetCore.Mvc;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.HistoryInventoryDTO;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryInventoryController : ControllerBase
    {
        private readonly IShopifyService _shopifyService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public HistoryInventoryController(IShopifyService shopifyService, ICommonService commonService)
        {
            _shopifyService = shopifyService;
            _commonService = commonService;
        }

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
