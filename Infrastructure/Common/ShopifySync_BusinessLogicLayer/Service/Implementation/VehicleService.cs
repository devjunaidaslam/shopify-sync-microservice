using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Repository.Implementation;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.ModelDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.VehicleDTO;
using ShopifySync_DataAccessLayer.Entities.Supplier;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;
using static ShopifySync_DataAccessLayer.Entities.Vehicle.VehicleModel;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class VehicleService : IVehicleService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;

        private readonly IVehicleRepository _repository;

        private readonly ITypeRepository  _typeRepository;
        private readonly IModelRepository _modelRepository;
        private readonly IMakeRepository  _makeRepository;
        private readonly IYearRepository  _yearRepository;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public VehicleService(
            ICommonService commonService,
            IVehicleRepository repository,
            ITypeRepository tyeRepository,
            IModelRepository modelRepository,
            IMakeRepository makeRepository,
            IYearRepository yearRepository,
            IMapper mapper,
            IConfiguration configuration
        )
        {
            _commonService = commonService;
            _repository = repository;
            _typeRepository = tyeRepository;
            _modelRepository  = modelRepository;
            _makeRepository = makeRepository;
            _yearRepository   = yearRepository;
            _mapper = mapper;
            _configuration = configuration;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");

        }
        #endregion

        public async Task<Response> GetAllVehicleList(VehicleFilterModel vehicleFilterModel)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var vehicleResponse = await _repository.GetAllVehicleList(vehicleFilterModel);
                var result = _mapper.Map<IEnumerable<VehicleDTO>>(vehicleResponse);

                int totalItemCount = vehicleResponse is IPagedList pagedList
                    ? pagedList.TotalItemCount
                    : result.Count();

                int currentPageNo = vehicleFilterModel.page_no > 0 ? vehicleFilterModel.page_no.Value : 1;
                int perPage = DefaultPageSize;

                if (!string.IsNullOrEmpty(vehicleFilterModel.per_page) && vehicleFilterModel.per_page.ToLower() != "all")
                {
                    int.TryParse(vehicleFilterModel.per_page, out perPage);
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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleDetailsMessage, result, currentUserRole, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllVehicleList", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }


        public async Task<Response> GetVehicleDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var vehicleResponse = await _repository.GetVehicleDetailsById(id);
                var result = _mapper.Map<VehicleDTO>(vehicleResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.VehicleDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchVehicleDetailsMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetVehicleDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewVehicle(VehicleCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                #region Checking if provided typeId,makeId,yearId,modelId exsisit in DB or not
                var typeId = await _typeRepository.GetVehicleTypeDetailsById(dto.TypeId);
                var modelId = await _modelRepository.GetVehicleModelDetailsById(dto.ModelId);
                var makeId = await _makeRepository.GetVehicleMakeDetailsById(dto.MakeId);
                var yearId = await _yearRepository.GetVehicleYearDetailsById(dto.YearId);
                #endregion

                if (typeId == null || modelId == null || makeId == null || yearId == null)
                {
                    var errorMessage = typeId == null ? _ApiResponseMessageList.ProperTypeIdMessage :
                       modelId == null ? _ApiResponseMessageList.ProperModelIdMessage :
                       makeId == null ? _ApiResponseMessageList.ProperMakeIdMessage :
                       _ApiResponseMessageList.ProperYearIdMessage; // last one if others are not null

                    return ResponseHelper.Success(errorMessage, null, currentUserRole);
                }

                var vehicleDetails = _mapper.Map<Vehicles>(dto);
                await _repository.CreateNewVehicle(vehicleDetails);
                // Getting inserted vehicle details 
                var vehicleResponse = await _repository.GetVehicleDetailsById(vehicleDetails.VehicleId);
                var result = _mapper.Map<VehicleDTO>(vehicleResponse);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateVehicle(VehicleUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var vehicleDetails = await _repository.GetVehicleDetailsByIdForDelete(dto.VehicleId);
                _mapper.Map(dto, vehicleDetails);
                await _repository.UpdateVehicle(vehicleDetails);
                var result = _mapper.Map<VehicleDTO>(vehicleDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleDetailsUpdatedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteVehicle(long id)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteVehicle(id);

                return ResponseHelper.Success(_ApiResponseMessageList.VehicleDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteVehicle", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> NavigateVehicleHierarchy(long? typeId, long? yearId, long? makeId)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                
                // Step 1: If no parameters provided, return all vehicle types
                if (!typeId.HasValue)
                {
                    // Get all vehicle types from the repository
                    var types = await _repository.GetAllVehicleTypes();
                    
                    return ResponseHelper.Success("Vehicle types retrieved successfully", new { types, level = "types" }, currentUserRole);
                }
                
                // Step 2: If only typeId is provided, return all years for that type
                if (typeId.HasValue && !yearId.HasValue)
                {
                    // Find all vehicles with the specified type and get their unique years
                    var years = await _repository.GetYearsByTypeId(typeId.Value);
                    
                    return ResponseHelper.Success("Years retrieved for the selected type", 
                        new { typeId, years, level = "years" }, currentUserRole);
                }
                
                // Step 3: If typeId and yearId are provided, return all makes for that type/year
                if (typeId.HasValue && yearId.HasValue && !makeId.HasValue)
                {
                    var makes = await _repository.GetMakesByTypeIdAndYearId(typeId.Value, yearId.Value);
                    
                    return ResponseHelper.Success("Makes retrieved for the selected type and year", 
                        new { typeId, yearId, makes, level = "makes" }, currentUserRole);
                }
                
                // Step 4: If typeId, yearId, and makeId are provided, return all models
                if (typeId.HasValue && yearId.HasValue && makeId.HasValue)
                {
                    var models = await _repository.GetModelsByTypeIdYearIdAndMakeId(typeId.Value, yearId.Value, makeId.Value);
                    
                    return ResponseHelper.Success("Models retrieved for the selected type, year, and make", 
                        new { typeId, yearId, makeId, models, level = "models" }, currentUserRole);
                }
                
                return ResponseHelper.BadRequest("Invalid parameter combination");
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "NavigateVehicleHierarchy", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> UpdateResourceActiveStatus(UpdateResourceActiveStatusDTO dto)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                
                // Validate resource type
                var validResourceTypes = new[] { "type", "year", "make", "model" };
                if (!validResourceTypes.Contains(dto.ResourceType.ToLower()))
                {
                    return ResponseHelper.BadRequest("Invalid resource type. Valid types are: type, year, make, model");
                }
                
                // Update the resource and cascade to vehicles
                int affectedVehicles = await _repository.UpdateResourceActiveStatus(dto.ResourceId, dto.ResourceType, dto.IsActive);
                
                string resourceTypeName = dto.ResourceType.ToLower() switch
                {
                    "type" => "Vehicle Type",
                    "year" => "Vehicle Year",
                    "make" => "Vehicle Make",
                    "model" => "Vehicle Model",
                    _ => dto.ResourceType
                };
                
                string statusText = dto.IsActive ? "activated" : "deactivated";
                string message = $"{resourceTypeName} has been {statusText} successfully. {affectedVehicles} vehicle(s) were also updated.";
                
                var result = new
                {
                    ResourceId = dto.ResourceId,
                    ResourceType = dto.ResourceType,
                    IsActive = dto.IsActive,
                    AffectedVehiclesCount = affectedVehicles
                };
                
                return ResponseHelper.Success(message, result, currentUserRole);
            }
            catch (ArgumentException ex)
            {
                return ResponseHelper.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateResourceActiveStatus", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

    }


}
