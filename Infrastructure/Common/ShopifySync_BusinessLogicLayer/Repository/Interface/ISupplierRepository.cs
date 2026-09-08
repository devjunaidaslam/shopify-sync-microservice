using ShopifySync_DataAccessLayer.Entities.DTOs.Supplier;
using ShopifySync_DataAccessLayer.Entities.DTOs.VehicleDTO;
using ShopifySync_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface ISupplierRepository
    {
        Task<IEnumerable<Suppliers>> GetAllSupplierList(string search, int pageNo, string pageSize);
        Task<Suppliers> GetSupplierDetailsById(long id);
        Task<Suppliers?> GetSupplierByCodeAsync(string code);
        Task CreateNewSupplier(Suppliers supplier);
        Task UpdateSupplier(Suppliers supplier);
        Task DeleteSupplier(long supplierId);
    }
}
