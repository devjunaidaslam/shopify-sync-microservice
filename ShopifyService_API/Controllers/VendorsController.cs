using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize = 20;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="VendorsController"/> class.
        /// </summary>
        /// <param name="vendorService">Service for vendor operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public VendorsController(IVendorService vendorService, ICommonService commonService)
        {
            _vendorService = vendorService;
            _commonService = commonService;
        }

        /// <summary>
        /// Retrieves a list of vendors with optional search and pagination.
        /// </summary>
        /// <param name="search">Search term for filtering vendors.</param>
        /// <param name="page_no">Page number for pagination (default is 1).</param>
        /// <param name="per_page">Number of items per page (default is "all").</param>
        /// <returns>A list of vendors or an error response.</returns>
        [HttpGet]
        public async Task<IActionResult> GetVendors(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _vendorService.GetVendorsAsync(search, page_no, per_page);
                return (result.IsSuccess == true) ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVendors", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
