using Microsoft.Extensions.Configuration;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class VendorService : IVendorService
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly ICommonService _commonService;
        private readonly int DefaultPageSize;
        private readonly IConfiguration _configuration;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public VendorService(IVendorRepository vendorRepository, ICommonService commonService, IConfiguration configuration)
        {
            _commonService = commonService;
            _vendorRepository = vendorRepository;
            _configuration = configuration;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }

        public async Task<Response> GetVendorsAsync(string search, int pageNo, string pageSize)
        {
            try
            {
                var vendors = await _vendorRepository.GetVendorsAsync(search, pageNo, pageSize);
                var totalItemCount = vendors.Count();

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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVendorMessage, vendors, null, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVendorsAsync", 1, ex.Message, ex.ToString());
                throw;
            }
        }
    }
}
