using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Repository.Interface
{
    public interface IProductRepository
    {
        Task<(int TotalCount, List<ProductDto> Data)> GetAllProductsPaginatedAsync(ProductFilterDto productFilter);
        Task<ProductByIdDto?> GetProductByIdAsync(int productId);
        Task<int> GetTotalCountAsync(string search);

    }
}
