using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.Supplier;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
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
