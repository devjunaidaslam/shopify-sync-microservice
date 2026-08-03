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
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public CollectionsController(ICollectionService collectionService, ICommonService commonService)
        {
            _collectionService = collectionService;
            _commonService = commonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCollections(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _collectionService.GetCollectionsAsync(search ?? "", page_no, per_page ?? "all");
                return result.IsSuccess == true ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetCollections", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
