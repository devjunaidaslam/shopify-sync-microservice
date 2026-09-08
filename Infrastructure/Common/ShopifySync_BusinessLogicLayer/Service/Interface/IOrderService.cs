using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IOrderService
    {
        Task<Response> GetAllOrdersPaginatedAsync(OrderFilterDTO filter);
        Task<Response> GetOrderByIdAsync(long orderId);
        Task<Response> CreateOrderAsync(OrderCreateDTO orderCreateDto);
        Task<Response> UpdateOrderAsync(OrderUpdateDTO orderUpdateDto);
        Task<Response> DeleteOrderAsync(long orderId);
        Task<Response> GetOrderByShopifyOrderIdAsync(long shopifyOrderId);
        Task<Response> CreateOrderActionAsync(OrderActionCreateDTO orderActionCreateDto);
        Task<Response> GetOrderActionsByOrderIdAsync(long orderId);
        Task<Response> ImportOrderFromShopifyAsync(ShopifyOrderImportRequestDTO request);
    }
}
