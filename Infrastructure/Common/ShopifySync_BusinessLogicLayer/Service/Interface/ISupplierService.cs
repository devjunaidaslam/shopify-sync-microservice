using Microsoft.AspNetCore.Http;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.Supplier;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;
using ShopifySync_DataAccessLayer.Entities.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ISupplierService
    {
        Task<Response> GetAllSupplierList(string search, int pageNo, string pageSize);
        Task<Response> GetSupplierDetailsById(long id);
        Task<Response> CreateNewSupplier(SupplierCreateDTO dto);
        Task<Response> UpdateSupplier(SupplierUpdateDTO dto);
        Task<Response> DeleteSupplier(long supplierId);

    }
}
