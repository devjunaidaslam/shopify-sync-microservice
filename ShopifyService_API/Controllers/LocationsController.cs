using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public LocationsController(ILocationService locationService, ICommonService commonService)
        {
            _locationService = locationService;
            _commonService = commonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _locationService.GetLocationsAsync(search ?? "", page_no, per_page ?? "all");
                return result.IsSuccess == true ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetLocations", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
