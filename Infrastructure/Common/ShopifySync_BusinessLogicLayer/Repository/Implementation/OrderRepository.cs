using Microsoft.EntityFrameworkCore;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ShopifySyncDbContext _context;

        public OrderRepository(ShopifySyncDbContext context)
        {
            _context = context;
        }

        public async Task<(int TotalCount, List<OrderDTO> Data)> GetAllOrdersPaginatedAsync(OrderFilterDTO filter)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.LineItems)
                .Include(o => o.FulfillmentOrders)
                    .ThenInclude(fo => fo.LineItems)
                .Include(o => o.Actions)
                .AsSplitQuery();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.search))
            {
                string searchTerm = filter.search.ToLower();
                query = query.Where(o =>
                    o.Name.ToLower().Contains(searchTerm) ||
                    o.ShopifyOrderId.ToString().Contains(searchTerm) ||
                    (o.Customer != null && (
                        o.Customer.FirstName.ToLower().Contains(searchTerm) ||
                        o.Customer.LastName.ToLower().Contains(searchTerm) ||
                        o.Customer.Email.ToLower().Contains(searchTerm)
                    ))
                );
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
            }

            if (!string.IsNullOrEmpty(filter.SourceName))
            {
                query = query.Where(o => o.SourceName == filter.SourceName);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAtShopify >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(o => o.CreatedAtShopify <= filter.ToDate.Value);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((filter.page_no - 1) * filter.page_size)
                .Take(filter.page_size)
                .ToListAsync();

            var orderDtos = orders.Select(MapToOrderDTO).ToList();

            return (totalCount, orderDtos);
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(long orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.LineItems)
                .Include(o => o.FulfillmentOrders)
                    .ThenInclude(fo => fo.LineItems)
                .Include(o => o.Actions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order != null ? MapToOrderDTO(order) : null;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> UpdateOrderAsync(Order order)
        {
            var existingOrder = await _context.Orders.FindAsync(order.Id);
            if (existingOrder == null)
                return null;

            existingOrder.SourceName = order.SourceName;
            existingOrder.SaleLocationId = order.SaleLocationId;
            existingOrder.SaleLocationName = order.SaleLocationName;
            existingOrder.Currency = order.Currency;
            existingOrder.TotalPrice = order.TotalPrice;
            existingOrder.RawPayload = order.RawPayload;
            existingOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<bool> DeleteOrderAsync(long orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Order?> GetOrderByShopifyOrderIdAsync(long shopifyOrderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.LineItems)
                .Include(o => o.FulfillmentOrders)
                    .ThenInclude(fo => fo.LineItems)
                .Include(o => o.Actions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.ShopifyOrderId == shopifyOrderId);
        }

        public async Task<OrderAction> CreateOrderActionAsync(OrderAction orderAction)
        {
            _context.OrderActions.Add(orderAction);
            await _context.SaveChangesAsync();
            return orderAction;
        }

        public async Task<List<OrderActionDTO>> GetOrderActionsByOrderIdAsync(long orderId)
        {
            var actions = await _context.OrderActions
                .Where(a => a.OrderId == orderId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return actions.Select(a => new OrderActionDTO
            {
                Id = a.Id,
                OrderId = a.OrderId,
                FulfillmentOrderId = a.FulfillmentOrderId,
                ActionType = a.ActionType,
                Reason = a.Reason,
                FromLocationId = a.FromLocationId,
                ToLocationId = a.ToLocationId,
                Payload = a.Payload,
                IdempotencyKey = a.IdempotencyKey,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        private OrderDTO MapToOrderDTO(Order order)
        {
            return new OrderDTO
            {
                Id = order.Id,
                ShopifyOrderId = order.ShopifyOrderId,
                ShopifyOrderGid = order.ShopifyOrderGid,
                Name = order.Name,
                SourceName = order.SourceName,
                SaleLocationId = order.SaleLocationId,
                SaleLocationName = order.SaleLocationName,
                CustomerId = order.CustomerId,
                Customer = order.Customer != null ? new CustomerDTO
                {
                    Id = order.Customer.Id,
                    ShopifyCustomerId = order.Customer.ShopifyCustomerId,
                    FirstName = order.Customer.FirstName,
                    LastName = order.Customer.LastName,
                    Email = order.Customer.Email,
                    Phone = order.Customer.Phone,
                    Address1 = order.Customer.Address1,
                    Address2 = order.Customer.Address2,
                    City = order.Customer.City,
                    Province = order.Customer.Province,
                    CountryCode = order.Customer.CountryCode,
                    Zip = order.Customer.Zip,
                    CreatedAt = order.Customer.CreatedAt,
                    UpdatedAt = order.Customer.UpdatedAt
                } : null,
                CreatedAtShopify = order.CreatedAtShopify,
                Currency = order.Currency,
                TotalPrice = order.TotalPrice,
                RawPayload = order.RawPayload,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                LineItems = order.LineItems?.Select(li => new OrderLineItemDTO
                {
                    Id = li.Id,
                    OrderId = li.OrderId,
                    ShopifyLineItemId = li.ShopifyLineItemId,
                    ProductId = li.ProductId,
                    VariantId = li.VariantId,
                    Sku = li.Sku,
                    Title = li.Title,
                    Quantity = li.Quantity
                }).ToList() ?? new List<OrderLineItemDTO>(),
                FulfillmentOrders = order.FulfillmentOrders?.Select(fo => new FulfillmentOrderDTO
                {
                    Id = fo.Id,
                    OrderId = fo.OrderId,
                    ShopifyFulfillmentOrderGid = fo.ShopifyFulfillmentOrderGid,
                    Status = fo.Status,
                    AssignedLocationId = fo.AssignedLocationId,
                    AssignedLocationName = fo.AssignedLocationName,
                    DestinationFirstName = fo.DestinationFirstName,
                    DestinationLastName = fo.DestinationLastName,
                    DestinationEmail = fo.DestinationEmail,
                    DestinationPhone = fo.DestinationPhone,
                    DestinationAddress1 = fo.DestinationAddress1,
                    DestinationAddress2 = fo.DestinationAddress2,
                    DestinationCity = fo.DestinationCity,
                    DestinationProvince = fo.DestinationProvince,
                    DestinationCountryCode = fo.DestinationCountryCode,
                    DestinationZip = fo.DestinationZip,
                    RawPayload = fo.RawPayload,
                    CreatedAt = fo.CreatedAt,
                    UpdatedAt = fo.UpdatedAt,
                    LineItems = fo.LineItems?.Select(foli => new FulfillmentOrderLineItemDTO
                    {
                        Id = foli.Id,
                        FulfillmentOrderId = foli.FulfillmentOrderId,
                        ShopifyFoLineItemGid = foli.ShopifyFoLineItemGid,
                        ShopifyOrderLineItemId = foli.ShopifyOrderLineItemId,
                        Sku = foli.Sku,
                        Name = foli.Name,
                        Quantity = foli.Quantity
                    }).ToList() ?? new List<FulfillmentOrderLineItemDTO>()
                }).ToList() ?? new List<FulfillmentOrderDTO>(),
                Actions = order.Actions?.Select(a => new OrderActionDTO
                {
                    Id = a.Id,
                    OrderId = a.OrderId,
                    FulfillmentOrderId = a.FulfillmentOrderId,
                    ActionType = a.ActionType,
                    Reason = a.Reason,
                    FromLocationId = a.FromLocationId,
                    ToLocationId = a.ToLocationId,
                    Payload = a.Payload,
                    IdempotencyKey = a.IdempotencyKey,
                    CreatedAt = a.CreatedAt
                }).ToList() ?? new List<OrderActionDTO>()
            };
        }
    }
}
