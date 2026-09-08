using ShopifySync_DataAccessLayer.Entities.DTOs.ProductDTO;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface IProductRepository
    {
        Task<(int TotalCount, List<ProductDto> Data)> GetAllProductsPaginatedAsync(ProductFilterDto productFilter);
        Task<ProductByIdDto?> GetProductByIdAsync(int productId);
        Task<int> GetTotalCountAsync(string search);

    }
}
