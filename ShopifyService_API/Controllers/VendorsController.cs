using Microsoft.AspNetCore.Mvc;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ICommonService _commonService;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public VendorsController(IVendorService vendorService, ICommonService commonService)
        {
            _vendorService = vendorService;
            _commonService = commonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVendors(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _vendorService.GetVendorsAsync(search ?? "", page_no, per_page ?? "all");
                return result.IsSuccess == true ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVendors", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
