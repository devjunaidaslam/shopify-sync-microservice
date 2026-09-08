using ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Repository.Interface
{
    public interface IOrderRepository
    {
        Task<(int TotalCount, List<OrderDTO> Data)> GetAllOrdersPaginatedAsync(OrderFilterDTO filter);
        Task<OrderDTO?> GetOrderByIdAsync(long orderId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(long orderId);
        Task<Order?> GetOrderByShopifyOrderIdAsync(long shopifyOrderId);
        Task<OrderAction> CreateOrderActionAsync(OrderAction orderAction);
        Task<List<OrderActionDTO>> GetOrderActionsByOrderIdAsync(long orderId);
    }
}
