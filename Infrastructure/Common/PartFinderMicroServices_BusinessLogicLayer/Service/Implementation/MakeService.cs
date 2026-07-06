using AutoMapper;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TypeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.MakeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.Display;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using X.PagedList;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using System.Security.Claims;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class MakeService : IMakeService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IMakeRepository _repository;
        private readonly IMapper _mapper;
        private readonly int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public MakeService(
            IConfiguration configuration,
            ICommonService commonService,
            IMakeRepository repository,
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


        public async Task<Response> GetAllVehicleMakes(string search, int pageNo, string pageSize)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);

                var makeResponse = await _repository.GetAllVehicleMakes(search, pageNo, pageSize);
                var result = _mapper.Map<IEnumerable<MakeDTO>>(makeResponse);

                int totalItemCount = makeResponse is IPagedList pagedList
                    ? pagedList.TotalItemCount
                    : result.Count();

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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleMakeDetailsMessage, result, currentUserRole, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllVehicleMakes", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> GetVehicleMakeDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role); 
                var makeResponse = await _repository.GetVehicleMakeDetailsById(id);
                var result = _mapper.Map<MakeDTO>(makeResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.VehicleMakeDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleMakeDetailsMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVehicleMakeDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewVehicleMake(MakeCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var makeDetails = _mapper.Map<VehicleMakes>(dto);
                await _repository.CreateNewVehicleMake(makeDetails);
                var result = _mapper.Map<MakeDTO>(makeDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleMakeDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewVehicleMake", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateVehicleMake(MakeUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var makeDetails = await _repository.GetVehicleMakeDetailsById(dto.VehicleMakeId);
                _mapper.Map(dto, makeDetails);
                await _repository.UpdateVehicleMake(makeDetails);
                var result = _mapper.Map<MakeDTO>(makeDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleMakeDetailsUpdateMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVehicleMake", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> DeleteVehicleMake(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteVehicleMake(id);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleMakeDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteVehicleMake", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }
    }
}
