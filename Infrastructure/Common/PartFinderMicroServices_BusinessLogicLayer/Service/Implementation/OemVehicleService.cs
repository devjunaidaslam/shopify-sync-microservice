using Microsoft.Extensions.Configuration;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OEMVehicleDTO;
using System;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class OemVehicleService : IOemVehicleService
    {
        private readonly IOemVehicleRepository _repo;
        private readonly ICommonService _commonService;
        private readonly IConfiguration _configuration;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();

        public OemVehicleService(IOemVehicleRepository repo, ICommonService commonService, IConfiguration configuration)
        {
            _repo = repo;
            _commonService = commonService;
            _configuration = configuration;
        }

        public async Task<Response> AttachAsync(OemVehicleLinkDTO dto)
        {
            try
            {
                var result = await _repo.AttachAsync(dto.OEMId, dto.VehicleId, dto.SupplierId);
                return ResponseHelper.Success("OEM attached to vehicle successfully", result);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", nameof(AttachAsync), 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }

        public async Task<Response> DetachAsync(OemVehicleLinkDTO dto)
        {
            try
            {
                var success = await _repo.DetachAsync(dto.OEMId, dto.VehicleId);
                if (!success)
                {
                    return ResponseHelper.NotFound("Attachment not found for given OEM and Vehicle");
                }
                return ResponseHelper.Success("OEM detached from vehicle successfully", success);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", nameof(DetachAsync), 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }
    }
}
