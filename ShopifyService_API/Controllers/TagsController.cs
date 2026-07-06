using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize = 20;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="TagsController"/> class.
        /// </summary>
        /// <param name="tagService">Service for tag operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public TagsController(ITagService tagService,ICommonService commonService)
        {
            _tagService = tagService;
            _commonService = commonService;
        }

        /// <summary>
        /// Retrieves a list of tags with optional search and pagination.
        /// </summary>
        /// <param name="search">Search term for filtering tags.</param>
        /// <param name="page_no">Page number for pagination (default is 1).</param>
        /// <param name="per_page">Number of items per page (default is "all").</param>
        /// <returns>A list of tags or an error response.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTags(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _tagService.GetTagsAsync(search, page_no, per_page);

                return (result.IsSuccess == true) ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetTags", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
} 