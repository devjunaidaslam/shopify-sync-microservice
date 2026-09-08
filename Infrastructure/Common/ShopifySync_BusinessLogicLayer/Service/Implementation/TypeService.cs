using AutoMapper;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.YearDTO;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using X.PagedList;
using ShopifySync_BusinessLogicLayer.Functions;
using System.Security.Claims;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class TypeService : ITypeService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;

        private readonly ITypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public TypeService(
            IConfiguration configuration,
            ICommonService commonService,
            ITypeRepository repository,
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


        public async Task<Response> GetAllVehicleType(string search, int pageNo, string pageSize)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var typeResponse = await _repository.GetAllVehicleTypes(search, pageNo, pageSize);
                var result = _mapper.Map<IEnumerable<TypeDTO>>(typeResponse);

                int totalItemCount = typeResponse is IPagedList pagedList
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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleTypeDetailsMessage, result, currentUserRole, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllVehicleType", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }


        public async Task<Response> GetVehicleTypeDetailsId(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var typeResponse = await _repository.GetVehicleTypeDetailsById(id);
                var result = _mapper.Map<TypeDTO>(typeResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.VehicleTypeDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleTypeDetailsMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVehicleTypeDetailsId", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewVehicleType(TypeCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var typeDetails = _mapper.Map<VehicleTypes>(dto);
                await _repository.CreateNewVehicleType(typeDetails);
                var result = _mapper.Map<TypeDTO>(typeDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewVehicleType", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateVehicleType(TypeUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var typeDetails = await _repository.GetVehicleTypeDetailsById(dto.VehicleTypesId);
                _mapper.Map(dto, typeDetails);
                await _repository.UpdateVehicleType(typeDetails);
                var result = _mapper.Map<TypeDTO>(typeDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsUpdateMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVehicleType", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteVehicleType(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteVehicleType(id);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleTypeDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteVehicleType", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }
    }

}
