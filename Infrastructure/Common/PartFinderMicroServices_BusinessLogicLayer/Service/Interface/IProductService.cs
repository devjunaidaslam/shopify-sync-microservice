using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IProductService
    {
        Task<Response> GetAllProductsPaginatedAsync(ProductFilterDto productFilterDto);
        Task<Response> GetProductAsync(int productId);
    }
}
