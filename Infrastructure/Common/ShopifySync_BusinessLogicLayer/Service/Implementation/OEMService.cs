using Microsoft.Extensions.Configuration;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.OEMDTO;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class OEMService : IOEMService
    {
        #region Fields
        private readonly IOEMRepository _oemRepository;
        private readonly ICommonService _commonService;
        private readonly IConfiguration _configuration;
        private readonly int DefaultPageSize;
        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public OEMService(IOEMRepository oemRepository, ICommonService commonService, IConfiguration configuration)
        {
            _oemRepository = oemRepository;
            _commonService = commonService;
            _configuration = configuration;
            DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize");
        }
        #endregion

        public async Task<Response> GetAllOEMList(OEMFilterModel oemFilterModel)
        {
            Response response = new Response();
            try
            {
                var result = await _oemRepository.GetAllOEMList(oemFilterModel);
                if (result != null)
                {
                    int totalItemCount = result is IPagedList pagedList
                        ? pagedList.TotalItemCount
                        : result.Count();

                    int perPage = DefaultPageSize;
                    if (!string.IsNullOrEmpty(oemFilterModel.per_page) && oemFilterModel.per_page.ToLower() != "all")
                    {
                        int.TryParse(oemFilterModel.per_page, out perPage);
                    }
                    else if (!string.IsNullOrEmpty(oemFilterModel.per_page) && oemFilterModel.per_page.ToLower() == "all")
                    {
                        perPage = totalItemCount;
                    }
                    // If per_page is null/empty and not "all", perPage remains DefaultPageSize

                    int pageNo = oemFilterModel.page_no > 0 ? (int)oemFilterModel.page_no : 1;
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

                    return ResponseHelper.Success(_ApiResponseMessageList.FetchOEMDetailsMessage, result, null, pagination);
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMDetailsNotFoundMessage;
                    response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
            return response;
        }

        public async Task<Response> GetOEMDetailsById(int id)
        {
            Response response = new Response();
            try
            {
                var result = await _oemRepository.GetOEMDetailsById(id);
                if (result != null)
                {
                    response.IsSuccess = true;
                    response.Message = _ApiResponseMessageList.FetchOEMDetailsMessage;
                    response.StatusCode = 200;
                    response.Data = result;
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMDetailsNotFoundMessage;
                    response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
            return response;
        }

        public async Task<Response> CreateNewOEM(OEMCreateDTO dto)
        {
            Response response = new Response();
            try
            {
                // Check if OEM name already exists
                var nameExists = await _oemRepository.OEMNameExists(dto.Name);
                if (nameExists)
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMNameAlreadyExistsMessage;
                    response.StatusCode = 400;
                    return response;
                }

                var result = await _oemRepository.CreateNewOEM(dto);
                if (result != null)
                {
                    response.IsSuccess = true;
                    response.Message = _ApiResponseMessageList.OEMDetailsAddedMessage;
                    response.StatusCode = 201;
                    response.Data = result;
                }
                else
                {
                    return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
            return response;
        }

        public async Task<Response> UpdateOEM(OEMUpdateDTO dto)
        {
            Response response = new Response();
            try
            {
                // Check if OEM exists
                var exists = await _oemRepository.OEMExists(dto.Id);
                if (!exists)
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMDetailsNotFoundMessage;
                    response.StatusCode = 404;
                    return response;
                }

                // Check if OEM name already exists (excluding current OEM)
                var nameExists = await _oemRepository.OEMNameExists(dto.Name, dto.Id);
                if (nameExists)
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMNameAlreadyExistsMessage;
                    response.StatusCode = 400;
                    return response;
                }

                var result = await _oemRepository.UpdateOEM(dto);
                if (result != null)
                {
                    response.IsSuccess = true;
                    response.Message = _ApiResponseMessageList.OEMDetailsUpdatedMessage;
                    response.StatusCode = 200;
                    response.Data = result;
                }
                else
                {
                    return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
            return response;
        }

        public async Task<Response> DeleteOEM(int id)
        {
            Response response = new Response();
            try
            {
                // Check if OEM exists
                var exists = await _oemRepository.OEMExists(id);
                if (!exists)
                {
                    response.IsSuccess = false;
                    response.Message = _ApiResponseMessageList.OEMDetailsNotFoundMessage;
                    response.StatusCode = 404;
                    return response;
                }

                var result = await _oemRepository.DeleteOEM(id);
                if (result)
                {
                    response.IsSuccess = true;
                    response.Message = _ApiResponseMessageList.OEMDetailsRemoveMessage;
                    response.StatusCode = 200;
                }
                else
                {
                    return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
            return response;
        }
    }
}
