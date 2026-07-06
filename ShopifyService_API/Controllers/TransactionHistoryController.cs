using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;

namespace ShopifyService_API.Controllers
{
    /// <summary>
    /// Controller for managing Shopify transaction history
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionHistoryController : ControllerBase
    {
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly ICommonService _commonService;
        private readonly ResponseMessageList _apiResponseMessageList = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionHistoryController"/> class.
        /// </summary>
        /// <param name="transactionHistoryService">Service for transaction history operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public TransactionHistoryController(ITransactionHistoryService transactionHistoryService, ICommonService commonService)
        {
            _transactionHistoryService = transactionHistoryService;
            _commonService = commonService;
        }

        /// <summary>
        /// get transaction history with filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Cleans up old transaction history records based on retention policy.
        /// </summary>
        /// <param name="retentionDays">Number of days to retain records.</param>
        /// <returns>Number of records cleaned up or an error response.</returns>
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
