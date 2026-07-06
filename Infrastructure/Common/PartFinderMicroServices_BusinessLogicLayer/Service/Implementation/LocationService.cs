using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service for handling location-related business logic.
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize;
        private readonly IConfiguration _configuration;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationService"/> class.
        /// </summary>
        /// <param name="locationRepository">Repository for location data access.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        /// <param name="configuration">Configuration for pagination and other settings.</param>
        public LocationService(ILocationRepository locationRepository, ICommonService commonService,IConfiguration configuration)
        {
            _commonService = commonService;
            _locationRepository = locationRepository;
            _configuration = configuration;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }

        /// <summary>
        /// Retrieves a paginated list of locations based on search and pagination parameters.
        /// </summary>
        /// <param name="search">Search term for filtering locations.</param>
        /// <param name="pageNo">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page or "all" for all items.</param>
        /// <returns>A Response object containing the paginated list of locations and pagination info.</returns>
        public async Task<Response> GetLocationsAsync(string search, int pageNo, string pageSize)
        {
            try
            {
                var locations = await _locationRepository.GetLocationsAsync(search, pageNo, pageSize);
                var totalItemCount = locations.Count();

                int perPage = DefaultPageSize;
                if (!string.IsNullOrEmpty(pageSize) && pageSize.ToLower() != "all")
                {
                    int.TryParse(pageSize, out perPage);
                }
                else
                {
                    perPage = totalItemCount;
                }

                pageNo = pageNo > 0 ? pageNo : 1;
                int totalPages = perPage > 0 ? (int)Math.Ceiling((double)totalItemCount / perPage) : 0;

                var pagination = new PaginationInfo
                {
                    TotalItemCount = totalItemCount,
                    PageNo = pageNo,
                    PerPage = perPage,
                    TotalPages = totalPages,
                    NextPage = pageNo < totalPages ? pageNo + 1 : 0,
                    PrevPage = pageNo > 1 ? pageNo - 1 : 0
                };

                return ResponseHelper.Success(_ApiResponseMessageList.FetchLocationMessage, locations, null, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetLocationsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }
    }
} 