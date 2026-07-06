using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly ICollectionService _collectionService;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize = 20;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionsController"/> class.
        /// </summary>
        /// <param name="collectionService">Service for collection operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public CollectionsController(ICollectionService collectionService, ICommonService commonService)
        {
            _collectionService = collectionService;
            _commonService = commonService;
        }

        /// <summary>
        /// Retrieves a list of collections with optional search and pagination.
        /// </summary>
        /// <param name="search">Search term for filtering collections.</param>
        /// <param name="page_no">Page number for pagination (default is 1).</param>
        /// <param name="per_page">Number of items per page (default is "all").</param>
        /// <returns>A list of collections or an error response.</returns>
        [HttpGet]
        public async Task<IActionResult> GetCollections(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _collectionService.GetCollectionsAsync(search, page_no, per_page);
                return (result.IsSuccess == true) ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetCollections", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
} 