using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using AutoMapper;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.YearDTO;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using X.PagedList;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using System.Security.Claims;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class YearService : IYearService
    {

        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IYearRepository _repository;
        private readonly IMapper _mapper;
        private readonly int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public YearService(
            IConfiguration configuration,
            ICommonService commonService,
            IYearRepository repository,
            IMapper mapper
        )
        {
            _configuration = configuration;
            _commonService = commonService;
            _repository = repository;
            _mapper = mapper;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion


        public async Task<Response> GetAllVehicleYears(string search, int pageNo, string pageSize)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var yearResponse = await _repository.GetAllVehicleYears(search, pageNo, pageSize);
                var result = _mapper.Map<IEnumerable<YearDTO>>(yearResponse);

                int totalItemCount = yearResponse is IPagedList pagedList
                    ? pagedList.TotalItemCount
                    : result.Count();

                int currentPageNo = pageNo > 0 ? pageNo : 1;
                int perPage = DefaultPageSize;

                if (!string.IsNullOrEmpty(pageSize) && pageSize.ToLower() != "all")
                {
                    int.TryParse(pageSize, out perPage);
                }
                else
                {
                    perPage = totalItemCount;
                }

                int totalPages = perPage > 0 ? (int)Math.Ceiling((double)totalItemCount / perPage) : 0;

                var pagination = new PaginationInfo
                {
                    TotalItemCount = totalItemCount,
                    PageNo = currentPageNo,
                    PerPage = perPage,
                    TotalPages = totalPages,
                    NextPage = currentPageNo < totalPages ? currentPageNo + 1 : 0,
                    PrevPage = currentPageNo > 1 ? currentPageNo - 1 : 0
                };

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleYearDetailsMessage, result, currentUserRole, pagination);

            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllVehicleYears", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }


        public async Task<Response> GetVehicleYearDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var yearResponse = await _repository.GetVehicleYearDetailsById(id);
                var result = _mapper.Map<YearDTO>(yearResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.VehicleYearDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleYearDetailsMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVehicleYearDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewVehicleYear(YearCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var yearDetails = _mapper.Map<VehicleYears>(dto);
                await _repository.CreateNewVehicleYear(yearDetails);
                var result = _mapper.Map<YearDTO>(yearDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleYearDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewVehicleYear", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateVehicleYear(YearUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var yearDetails = await _repository.GetVehicleYearDetailsById(dto.VehicleYearId);
                _mapper.Map(dto, yearDetails);
                await _repository.UpdateVehicleYear(yearDetails);
                var result = _mapper.Map<YearDTO>(yearDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleYearDetailsUpdatedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVehicleYear", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteVehicleYear(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteVehicleYear(id);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleYearDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteVehicleYear", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }
    }
}
