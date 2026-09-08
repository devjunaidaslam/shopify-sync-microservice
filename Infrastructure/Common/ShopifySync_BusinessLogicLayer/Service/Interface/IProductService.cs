using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.ProductDTO;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IProductService
    {
        Task<Response> GetAllProductsPaginatedAsync(ProductFilterDto productFilterDto);
        Task<Response> GetProductAsync(int productId);
    }
}
