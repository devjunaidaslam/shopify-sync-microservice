using Microsoft.AspNetCore.Mvc;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionHistoryController : ControllerBase
    {
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly ICommonService _commonService;
        private readonly ResponseMessageList _apiResponseMessageList = new();

        public TransactionHistoryController(ITransactionHistoryService transactionHistoryService, ICommonService commonService)
        {
            _transactionHistoryService = transactionHistoryService;
            _commonService = commonService;
        }

        [HttpGet]
        public async Task<ActionResult<Response>> GetTransactions([FromQuery] TransactionFilterDto filter)
        {
            try
            {
                var response = await _transactionHistoryService.GetTransactionsAsync(filter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetTransactions", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        [HttpDelete("cleanup")]
        public async Task<ActionResult<Response>> CleanupOldRecords([FromQuery] int retentionDays = 30)
        {
            try
            {
                if (retentionDays <= 0)
                {
                    return BadRequest(ResponseHelper.BadRequest("Retention days must be greater than 0."));
                }

                var response = await _transactionHistoryService.CleanupOldRecordsAsync(retentionDays);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CleanupOldRecords", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
