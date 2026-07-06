using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using System;

namespace ShopifyService_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize = 20;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationService">Service for location operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public LocationsController(ILocationService locationService, ICommonService commonService)
        {
            _locationService = locationService;
            _commonService = commonService;
        }

        /// <summary>
        /// Retrieves a list of locations with optional search and pagination.
        /// </summary>
        /// <param name="search">Search term for filtering locations.</param>
        /// <param name="page_no">Page number for pagination (default is 1).</param>
        /// <param name="per_page">Number of items per page (default is "all").</param>
        /// <returns>A list of locations or an error response.</returns>
        [HttpGet]
        public async Task<IActionResult> GetLocations(string? search = "", int page_no = 1, string? per_page = "all")
        {
            try
            {
                var result = await _locationService.GetLocationsAsync(search, page_no, per_page);

                return (result.IsSuccess == true) ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetLocations", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_ApiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
