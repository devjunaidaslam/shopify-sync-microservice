using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Ocsp;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Infrastructure;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.Authentication.Register;
using ShopifySync_DataAccessLayer.Entities.DTOs.Supplier;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using ShopifySync_DataAccessLayer.Entities.Supplier;
using ShopifySync_DataAccessLayer.Enum;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;
using static ShopifySync_DataAccessLayer.Enum.ImportFitmentStatusEnum;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class SupplierService : ISupplierService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;

        private readonly ISupplierRepository _repository;
        private readonly IMapper _mapper;
        private readonly int DefaultPageSize;

        ResponseMessageList _ApiResponseMessageList = new ResponseMessageList();
        #endregion

        #region Constructor
        public SupplierService(
            IConfiguration configuration,
            ICommonService commonService,
            ISupplierRepository repository,
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


        public async Task<Response> GetAllSupplierList(string search, int pageNo, string pageSize)
        {
            Response response = new Response();

            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var supplierResponse = await _repository.GetAllSupplierList(search, pageNo, pageSize);
                var result = _mapper.Map<IEnumerable<SupplierDTO>>(supplierResponse);

                int totalItemCount = supplierResponse is IPagedList pagedList
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

                return ResponseHelper.Success(_ApiResponseMessageList.FetchSupplierDetailsMessage, result, currentUserRole, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllSupplierList", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }
        }


        public async Task<Response> GetSupplierDetailsById(long id)
        {
            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var supplierResponse = await _repository.GetSupplierDetailsById(id);
                var result = _mapper.Map<SupplierDTO>(supplierResponse);

                if (result == null)
                {
                    return ResponseHelper.NotFound(_ApiResponseMessageList.SupplierDetailsNotFoundMessage);
                }

                return ResponseHelper.Success(_ApiResponseMessageList.FetchSupplierDetailsMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetSupplierDetailsById", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }

        }

        public async Task<Response> CreateNewSupplier(SupplierCreateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var supplierDetails = _mapper.Map<Suppliers>(dto);
                await _repository.CreateNewSupplier(supplierDetails);
                var result = _mapper.Map<SupplierDTO>(supplierDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.SupplierDetailsAddedMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateNewSupplier", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> UpdateSupplier(SupplierUpdateDTO dto)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                var supplierDetails = await _repository.GetSupplierDetailsById(dto.SupplierId);
                _mapper.Map(dto, supplierDetails);
                await _repository.UpdateSupplier(supplierDetails);
                var result = _mapper.Map<SupplierDTO>(supplierDetails);

                return ResponseHelper.Success(_ApiResponseMessageList.SupplierDetailsUpdateMessage, result, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateSupplier", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

        public async Task<Response> DeleteSupplier(long supplierId)
        {

            Response response = new Response();
            try
            {
                var currentUserRole = CommonFunction.GetUserDataByToken(ClaimTypes.Role);
                await _repository.DeleteSupplier(supplierId);

                return ResponseHelper.Success(_ApiResponseMessageList.SupplierDetailsRemoveMessage, null, currentUserRole);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteSupplier", 1, ex.Message, ex.ToString());
                return ResponseHelper.InternalServerError(_ApiResponseMessageList.FailResponseMessage);
            }


        }

    }
}
