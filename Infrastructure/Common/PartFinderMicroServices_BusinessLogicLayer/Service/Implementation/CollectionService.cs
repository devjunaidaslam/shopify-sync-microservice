using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service for handling collection-related business logic.
    /// </summary>
    public class CollectionService : ICollectionService
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize;
        private readonly IConfiguration _configuration;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionService"/> class.
        /// </summary>
        /// <param name="collectionRepository">Repository for collection data access.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        /// <param name="configuration">Configuration for pagination and other settings.</param>
        public CollectionService(ICollectionRepository collectionRepository , ICommonService commonService, IConfiguration configuration)
        {
            _commonService = commonService;
            _collectionRepository = collectionRepository;
            _configuration = configuration;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }

        /// <summary>
        /// Retrieves a paginated list of collections based on search and pagination parameters.
        /// </summary>
        /// <param name="search">Search term for filtering collections.</param>
        /// <param name="pageNo">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page or "all" for all items.</param>
        /// <returns>A Response object containing the paginated list of collections and pagination info.</returns>
        public async Task<Response> GetCollectionsAsync(string search, int pageNo, string pageSize)
        {
            try
            {
                var collections = await _collectionRepository.GetCollectionsAsync(search, pageNo, pageSize);
                var totalItemCount = collections.Count();

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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchCollectionMessage, collections, null, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetCollectionsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }
    }
} 