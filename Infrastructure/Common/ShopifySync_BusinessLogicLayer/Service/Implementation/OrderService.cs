using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities;
using ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO;
using ShopifySync_DataAccessLayer.Model;
using System.Text.Json;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICommonService _commonService;
        private readonly ILogger<OrderService> _logger;
        private readonly IShopifyService _shopifyService;
        private readonly IShopifyRepository _shopifyRepository;
        private readonly IPredikoOrderRMQService _predikoOrderRMQService;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IProductRepository _productRepository;
        private readonly IServiceProvider _serviceProvider;

        public OrderService(IOrderRepository orderRepository, ICommonService commonService, ILogger<OrderService> logger, IShopifyService shopifyService, IShopifyRepository shopifyRepository, IPredikoOrderRMQService predikoOrderRMQService, ISupplierRepository supplierRepository, IProductRepository productRepository, IServiceProvider serviceProvider)
        {
            _orderRepository = orderRepository;
            _commonService = commonService;
            _logger = logger;
            _shopifyService = shopifyService;
            _shopifyRepository = shopifyRepository;
            _predikoOrderRMQService = predikoOrderRMQService;
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
            _serviceProvider = serviceProvider;
            
            _logger.LogInformation("[OrderService] Service initialized successfully");
        }

        public async Task<Response> GetAllOrdersPaginatedAsync(OrderFilterDTO filter)
        {
            try
            {
                _logger.LogInformation("[OrderService] Fetching orders with filter: {Filter}", filter);
                var data = await _orderRepository.GetAllOrdersPaginatedAsync(filter);
                var totalItemCount = data.TotalCount;
                int totalPages = filter.page_size > 0 ? (int)Math.Ceiling((double)totalItemCount / filter.page_size) : 0;
                
                var pagination = new PaginationInfo
                {
                    TotalItemCount = totalItemCount,
                    PageNo = filter.page_no,
                    PerPage = filter.page_size,
                    TotalPages = totalPages,
                    NextPage = filter.page_no < totalPages ? filter.page_no + 1 : 0,
                    PrevPage = filter.page_no > 1 ? filter.page_no - 1 : 0
                };
                
                _logger.LogInformation("[OrderService] Successfully fetched {TotalItemCount} orders", totalItemCount);
                return ResponseHelper.Success("Orders fetched successfully", data.Data, null, pagination);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllOrdersPaginatedAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error fetching orders: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<Response> GetOrderByIdAsync(long orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return ResponseHelper.BadRequest("Order ID is not valid");
                }
                
                _logger.LogInformation("[OrderService] Fetching order with ID: {OrderId}", orderId);
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                
                if (order == null)
                {
                    _logger.LogWarning("[OrderService] Order with ID {OrderId} not found", orderId);
                    return ResponseHelper.NotFound("Order not found");
                }
                
                _logger.LogInformation("[OrderService] Successfully fetched order with ID: {OrderId}", orderId);
                return ResponseHelper.Success("Order fetched successfully", order);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderByIdAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error fetching order with ID {OrderId}: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
        }

        public async Task<Response> CreateOrderAsync(OrderCreateDTO orderCreateDto)
        {
            try
            {
                _logger.LogInformation("[OrderService] Creating order with Shopify Order ID: {ShopifyOrderId}", orderCreateDto.ShopifyOrderId);
                
                // Check if order already exists
                var existingOrder = await _orderRepository.GetOrderByShopifyOrderIdAsync(orderCreateDto.ShopifyOrderId);
                if (existingOrder != null)
                {
                    _logger.LogWarning("[OrderService] Order with Shopify Order ID {ShopifyOrderId} already exists", orderCreateDto.ShopifyOrderId);
                    return ResponseHelper.BadRequest("Order with this Shopify Order ID already exists");
                }
                
                // Map DTO to entity
                var order = new Order
                {
                    ShopifyOrderId = orderCreateDto.ShopifyOrderId,
                    ShopifyOrderGid = orderCreateDto.ShopifyOrderGid,
                    Name = orderCreateDto.Name,
                    SourceName = orderCreateDto.SourceName,
                    SaleLocationId = orderCreateDto.SaleLocationId,
                    SaleLocationName = orderCreateDto.SaleLocationName,
                    CustomerId = orderCreateDto.CustomerId,
                    CreatedAtShopify = orderCreateDto.CreatedAtShopify,
                    Currency = orderCreateDto.Currency,
                    TotalPrice = orderCreateDto.TotalPrice,
                    RawPayload = orderCreateDto.RawPayload,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Map Customer if provided
                if (orderCreateDto.Customer != null)
                {
                    order.Customer = new Customer
                    {
                        ShopifyCustomerId = orderCreateDto.Customer.ShopifyCustomerId,
                        FirstName = orderCreateDto.Customer.FirstName,
                        LastName = orderCreateDto.Customer.LastName,
                        Email = orderCreateDto.Customer.Email,
                        Phone = orderCreateDto.Customer.Phone,
                        Address1 = orderCreateDto.Customer.Address1,
                        Address2 = orderCreateDto.Customer.Address2,
                        City = orderCreateDto.Customer.City,
                        Province = orderCreateDto.Customer.Province,
                        CountryCode = orderCreateDto.Customer.CountryCode,
                        Zip = orderCreateDto.Customer.Zip,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                
                // Map Line Items
                if (orderCreateDto.LineItems != null && orderCreateDto.LineItems.Any())
                {
                    order.LineItems = orderCreateDto.LineItems.Select(li => new OrderLineItem
                    {
                        ShopifyLineItemId = li.ShopifyLineItemId,
                        ProductId = li.ProductId,
                        VariantId = li.VariantId,
                        Sku = li.Sku,
                        Title = li.Title,
                        Quantity = li.Quantity
                    }).ToList();
                }
                
                // Map Fulfillment Orders
                if (orderCreateDto.FulfillmentOrders != null && orderCreateDto.FulfillmentOrders.Any())
                {
                    order.FulfillmentOrders = orderCreateDto.FulfillmentOrders.Select(fo => new FulfillmentOrder
                    {
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
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LineItems = fo.LineItems?.Select(foli => new FulfillmentOrderLineItem
                        {
                            ShopifyFoLineItemGid = foli.ShopifyFoLineItemGid,
                            ShopifyOrderLineItemId = foli.ShopifyOrderLineItemId,
                            Sku = foli.Sku,
                            Name = foli.Name,
                            Quantity = foli.Quantity
                        }).ToList()
                    }).ToList();
                }
                
                var createdOrder = await _orderRepository.CreateOrderAsync(order);
                _logger.LogInformation("[OrderService] Successfully created order with ID: {OrderId}", createdOrder.Id);
                
                return ResponseHelper.Success("Order created successfully", createdOrder);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateOrderAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error creating order: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<Response> UpdateOrderAsync(OrderUpdateDTO orderUpdateDto)
        {
            try
            {
                _logger.LogInformation("[OrderService] Updating order with ID: {OrderId}", orderUpdateDto.Id);
                
                var order = new Order
                {
                    Id = orderUpdateDto.Id,
                    SourceName = orderUpdateDto.SourceName,
                    SaleLocationId = orderUpdateDto.SaleLocationId,
                    SaleLocationName = orderUpdateDto.SaleLocationName,
                    Currency = orderUpdateDto.Currency,
                    TotalPrice = orderUpdateDto.TotalPrice,
                    RawPayload = orderUpdateDto.RawPayload
                };
                
                var updatedOrder = await _orderRepository.UpdateOrderAsync(order);
                
                if (updatedOrder == null)
                {
                    _logger.LogWarning("[OrderService] Order with ID {OrderId} not found for update", orderUpdateDto.Id);
                    return ResponseHelper.NotFound("Order not found");
                }
                
                _logger.LogInformation("[OrderService] Successfully updated order with ID: {OrderId}", orderUpdateDto.Id);
                return ResponseHelper.Success("Order updated successfully", updatedOrder);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateOrderAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error updating order: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<Response> DeleteOrderAsync(long orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return ResponseHelper.BadRequest("Order ID is not valid");
                }
                
                _logger.LogInformation("[OrderService] Deleting order with ID: {OrderId}", orderId);
                var result = await _orderRepository.DeleteOrderAsync(orderId);
                
                if (!result)
                {
                    _logger.LogWarning("[OrderService] Order with ID {OrderId} not found for deletion", orderId);
                    return ResponseHelper.NotFound("Order not found");
                }
                
                _logger.LogInformation("[OrderService] Successfully deleted order with ID: {OrderId}", orderId);
                return ResponseHelper.Success("Order deleted successfully", null);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteOrderAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error deleting order with ID {OrderId}: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
        }

        public async Task<Response> GetOrderByShopifyOrderIdAsync(long shopifyOrderId)
        {
            try
            {
                _logger.LogInformation("[OrderService] Fetching order with Shopify Order ID: {ShopifyOrderId}", shopifyOrderId);
                var order = await _orderRepository.GetOrderByShopifyOrderIdAsync(shopifyOrderId);
                
                if (order == null)
                {
                    _logger.LogWarning("[OrderService] Order with Shopify Order ID {ShopifyOrderId} not found", shopifyOrderId);
                    return ResponseHelper.NotFound("Order not found");
                }
                
                _logger.LogInformation("[OrderService] Successfully fetched order with Shopify Order ID: {ShopifyOrderId}", shopifyOrderId);
                return ResponseHelper.Success("Order fetched successfully", order);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderByShopifyOrderIdAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error fetching order with Shopify Order ID {ShopifyOrderId}: {ErrorMessage}", shopifyOrderId, ex.Message);
                throw;
            }
        }

        public async Task<Response> CreateOrderActionAsync(OrderActionCreateDTO orderActionCreateDto)
        {
            try
            {
                _logger.LogInformation("[OrderService] Creating order action for Order ID: {OrderId}", orderActionCreateDto.OrderId);
                
                var orderAction = new OrderAction
                {
                    OrderId = orderActionCreateDto.OrderId,
                    FulfillmentOrderId = orderActionCreateDto.FulfillmentOrderId,
                    ActionType = orderActionCreateDto.ActionType,
                    Reason = orderActionCreateDto.Reason,
                    FromLocationId = orderActionCreateDto.FromLocationId,
                    ToLocationId = orderActionCreateDto.ToLocationId,
                    Payload = orderActionCreateDto.Payload,
                    IdempotencyKey = orderActionCreateDto.IdempotencyKey,
                    CreatedAt = DateTime.UtcNow
                };
                
                var createdAction = await _orderRepository.CreateOrderActionAsync(orderAction);
                _logger.LogInformation("[OrderService] Successfully created order action with ID: {ActionId}", createdAction.Id);
                
                return ResponseHelper.Success("Order action created successfully", createdAction);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateOrderActionAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error creating order action: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<Response> GetOrderActionsByOrderIdAsync(long orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return ResponseHelper.BadRequest("Order ID is not valid");
                }
                
                _logger.LogInformation("[OrderService] Fetching order actions for Order ID: {OrderId}", orderId);
                var actions = await _orderRepository.GetOrderActionsByOrderIdAsync(orderId);
                
                _logger.LogInformation("[OrderService] Successfully fetched {Count} order actions for Order ID: {OrderId}", actions.Count, orderId);
                return ResponseHelper.Success("Order actions fetched successfully", actions);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderActionsByOrderIdAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error fetching order actions for Order ID {OrderId}: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Imports an order from Shopify by fetching it via GraphQL and creating all related entities.
        /// Implements business logic for determining Transfer vs Drop Ship actions based on POS location routing.
        /// </summary>
        /// <param name="request">The import request containing Shopify order ID and optional supplier location names</param>
        /// <returns>Response containing the created order and actions</returns>
        public async Task<Response> ImportOrderFromShopifyAsync(ShopifyOrderImportRequestDTO request)
        {
            try
            {
                _logger.LogInformation("[OrderService] Starting import for Shopify Order ID: {ShopifyOrderId}", request.ShopifyOrderId);

                // Step 1: Fetch order data from Shopify GraphQL
                var shopifyData = await _shopifyService.FetchShopifyOrderByIdAsync(request.ShopifyOrderId);
                if (shopifyData == null)
                {
                    _logger.LogWarning("[OrderService] Order {ShopifyOrderId} not found in Shopify", request.ShopifyOrderId);
                    return ResponseHelper.NotFound($"Order {request.ShopifyOrderId} not found in Shopify");
                }

                var data = shopifyData.Value;
                var orderNode = data.GetProperty("order");
                var fulfillmentOrdersNode = data.GetProperty("fulfillmentOrders").GetProperty("nodes");

                // Step 2: Check if order already exists
                var existingOrder = await _orderRepository.GetOrderByShopifyOrderIdAsync(request.ShopifyOrderId);
                if (existingOrder != null)
                {
                    _logger.LogWarning("[OrderService] Order {ShopifyOrderId} already exists with ID {OrderId}", request.ShopifyOrderId, existingOrder.Id);
                    return ResponseHelper.BadRequest($"Order {request.ShopifyOrderId} already exists in the system");
                }

                // Step 3: Extract order details
                var orderGid = orderNode.GetProperty("id").GetString();
                var orderName = orderNode.GetProperty("name").GetString();
                var sourceName = orderNode.GetProperty("sourceName").GetString();
                var createdAt = orderNode.GetProperty("createdAt").GetDateTime();

                // Extract currency and total price
                string currency = null;
                decimal? totalPrice = null;
                if (orderNode.TryGetProperty("currencyCode", out var currencyProp))
                {
                    currency = currencyProp.GetString();
                }
                if (orderNode.TryGetProperty("totalPriceSet", out var totalPriceSet) && totalPriceSet.ValueKind != JsonValueKind.Null)
                {
                    if (totalPriceSet.TryGetProperty("shopMoney", out var shopMoney) && shopMoney.ValueKind != JsonValueKind.Null)
                    {
                        if (shopMoney.TryGetProperty("amount", out var amountProp))
                        {
                            if (decimal.TryParse(amountProp.GetString(), out var amount))
                            {
                                totalPrice = amount;
                            }
                        }
                        // Use currency from totalPriceSet if not already set
                        if (string.IsNullOrEmpty(currency) && shopMoney.TryGetProperty("currencyCode", out var currencyCodeProp))
                        {
                            currency = currencyCodeProp.GetString();
                        }
                    }
                }

                // Extract POS sale location if available
                long? posLocationId = null;
                string posLocationName = null;
                if (orderNode.TryGetProperty("retailLocation", out var retailLocationNode) && retailLocationNode.ValueKind != JsonValueKind.Null)
                {
                    var locationGid = retailLocationNode.GetProperty("id").GetString();
                    posLocationId = ExtractLocationIdFromGid(locationGid);
                    posLocationName = retailLocationNode.GetProperty("name").GetString();
                }

                _logger.LogDebug("[OrderService] Order {OrderName} | Source: {SourceName} | POS Location: {PosLocationId} | Currency: {Currency} | Total: {TotalPrice}", 
                    orderName, sourceName, posLocationId, currency, totalPrice);

                // Step 4: Process customer data
                Customer customer = null;
                if (orderNode.TryGetProperty("customer", out var customerNode) && customerNode.ValueKind != JsonValueKind.Null)
                {
                    customer = ProcessCustomerData(customerNode, orderNode);
                }

                // Step 5: Process order line items
                var lineItems = await ProcessOrderLineItems(orderNode);

                // Step 6: Process fulfillment orders and their line items
                var fulfillmentOrders = ProcessFulfillmentOrders(fulfillmentOrdersNode);

                // Step 7: Create the order entity
                var order = new Order
                {
                    ShopifyOrderId = request.ShopifyOrderId,
                    ShopifyOrderGid = orderGid,
                    Name = orderName,
                    SourceName = sourceName,
                    Currency = currency,
                    TotalPrice = totalPrice,
                    SaleLocationId = posLocationId,
                    SaleLocationName = posLocationName,
                    CreatedAtShopify = createdAt,
                    RawPayload = data.GetRawText(),
                    Customer = customer,
                    LineItems = lineItems,
                    FulfillmentOrders = fulfillmentOrders,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdOrder = await _orderRepository.CreateOrderAsync(order);
                _logger.LogInformation("[OrderService] Successfully created order with ID: {OrderId}", createdOrder.Id);

                // Step 8: Determine and create order actions based on business logic
                // Ensure supplier names are uppercase for case-insensitive comparison
                var supplierNames = request.SupplierLocationNames ?? new HashSet<string> { "FOURNISSEUR" };
                var supplierNamesUpper = new HashSet<string>(supplierNames.Select(s => s?.ToUpperInvariant() ?? ""), StringComparer.OrdinalIgnoreCase);
                var actionsCreated = await DetermineAndCreateOrderActions(createdOrder, fulfillmentOrdersNode, posLocationId, sourceName, supplierNamesUpper);

                // Step 9: Fetch the complete order with all relationships
                var completeOrder = await _orderRepository.GetOrderByIdAsync(createdOrder.Id);

                var response = new ShopifyOrderImportResponseDTO
                {
                    Order = completeOrder,
                    ActionsCreated = actionsCreated,
                    IsNewOrder = true,
                    Summary = $"Successfully imported order {orderName} with {lineItems.Count} line items, {fulfillmentOrders.Count} fulfillment orders, and {actionsCreated.Count} actions"
                };

                // Step 10: Publish order to Prediko queue (fire and forget, don't block order creation)
                var orderId = completeOrder.Id;
                var orderEntityId = createdOrder.Id;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Create a new scope for background work to avoid disposed DbContext
                        using var scope = _serviceProvider.CreateScope();
                        var supplierRepo = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();
                        var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                        var shopifyRepo = scope.ServiceProvider.GetRequiredService<IShopifyRepository>();
                        var predikoRMQ = scope.ServiceProvider.GetRequiredService<IPredikoOrderRMQService>();
                        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                        // Fetch the order again with the new scope
                        var orderDto = await orderRepo.GetOrderByIdAsync(orderEntityId);
                        if (orderDto == null)
                        {
                            _logger.LogWarning("[OrderService] Order {OrderId} not found when publishing to Prediko", orderEntityId);
                            return;
                        }

                        await PublishOrderToPredikoInternalAsync(orderDto, supplierRepo, productRepo, shopifyRepo, predikoRMQ);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[OrderService] Error publishing order {OrderId} to Prediko (non-blocking): {Message}", 
                            orderId, ex.Message);
                    }
                });

                _logger.LogInformation("[OrderService] Import completed for order {OrderName}: {Summary}", orderName, response.Summary);
                return ResponseHelper.Success("Order imported successfully from Shopify", response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportOrderFromShopifyAsync", 1, ex.Message, ex.ToString());
                _logger.LogError(ex, "[OrderService] Error importing order from Shopify: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        #region Private Helper Methods for Order Import

        /// <summary>
        /// Extracts numeric location ID from Shopify GID format (gid://shopify/Location/123 -> 123)
        /// </summary>
        private long? ExtractLocationIdFromGid(string gid)
        {
            if (string.IsNullOrEmpty(gid)) return null;
            var parts = gid.Split('/');
            if (parts.Length > 0 && long.TryParse(parts[^1], out var id))
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Extracts numeric ID from Shopify GID format
        /// </summary>
        private long ExtractIdFromGid(string gid)
        {
            if (string.IsNullOrEmpty(gid)) return 0;
            var parts = gid.Split('/');
            if (parts.Length > 0 && long.TryParse(parts[^1], out var id))
            {
                return id;
            }
            return 0;
        }

        /// <summary>
        /// Processes customer data from GraphQL response
        /// </summary>
        private Customer ProcessCustomerData(JsonElement customerNode, JsonElement orderNode)
        {
            var customer = new Customer
            {
                FirstName = customerNode.TryGetProperty("firstName", out var fn) ? fn.GetString() : null,
                LastName = customerNode.TryGetProperty("lastName", out var ln) ? ln.GetString() : null,
                Email = customerNode.TryGetProperty("email", out var em) ? em.GetString() : null,
                Phone = customerNode.TryGetProperty("phone", out var ph) ? ph.GetString() : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Try to get address from shippingAddress or billingAddress
            if (orderNode.TryGetProperty("shippingAddress", out var shippingAddr) && shippingAddr.ValueKind != JsonValueKind.Null)
            {
                customer.Address1 = shippingAddr.TryGetProperty("address1", out var a1) ? a1.GetString() : null;
                customer.Address2 = shippingAddr.TryGetProperty("address2", out var a2) ? a2.GetString() : null;
                customer.City = shippingAddr.TryGetProperty("city", out var c) ? c.GetString() : null;
                customer.Province = shippingAddr.TryGetProperty("province", out var p) ? p.GetString() : null;
                customer.CountryCode = shippingAddr.TryGetProperty("country", out var co) ? co.GetString() : null;
                customer.Zip = shippingAddr.TryGetProperty("zip", out var z) ? z.GetString() : null;
            }

            return customer;
        }

        /// <summary>
        /// Processes order line items from GraphQL response
        /// </summary>
        private async Task<List<OrderLineItem>> ProcessOrderLineItems(JsonElement orderNode)
        {
            var lineItems = new List<OrderLineItem>();

            if (orderNode.TryGetProperty("lineItems", out var lineItemsNode))
            {
                foreach (var liNode in lineItemsNode.GetProperty("nodes").EnumerateArray())
                {
                    var lineItemGid = liNode.GetProperty("id").GetString();
                    var sku = liNode.TryGetProperty("sku", out var skuProp) ? skuProp.GetString() : null;
                    
                    // Initialize variant and product IDs as null
                    long? localVariantId = null;
                    long? localProductId = null;
                    
                    // Try to get variant information
                    if (liNode.TryGetProperty("variant", out var variantNode) && variantNode.ValueKind != JsonValueKind.Null)
                    {
                        // Fallback to variant SKU if line item SKU is null
                        if (string.IsNullOrEmpty(sku))
                        {
                            sku = variantNode.TryGetProperty("sku", out var vSku) ? vSku.GetString() : null;
                        }
                        
                        // Get the Shopify variant ID
                        if (variantNode.TryGetProperty("id", out var variantIdNode))
                        {
                            var shopifyVariantGid = variantIdNode.GetString(); // e.g., "gid://shopify/ProductVariant/123456"
                            
                            // Look up the local variant by FULL Shopify GID (stored as full GID in database)
                            try
                            {
                                var localVariant = await _shopifyRepository.GetVariantByShopifyIdAsync(shopifyVariantGid);
                                if (localVariant != null)
                                {
                                    localVariantId = (long)localVariant.Id;
                                    localProductId = (long)localVariant.ProductId;
                                    
                                    _logger.LogDebug("[OrderService] Found local variant {VariantId} and product {ProductId} for Shopify variant GID: {ShopifyVariantGid}", 
                                        localVariantId, localProductId, shopifyVariantGid);
                                }
                                else
                                {
                                    _logger.LogWarning("[OrderService] Could not find local variant for Shopify variant GID: {ShopifyVariantGid}, SKU: {SKU}", 
                                        shopifyVariantGid, sku);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[OrderService] Error looking up variant for Shopify GID: {ShopifyVariantGid}", shopifyVariantGid);
                            }
                        }
                    }

                    var lineItem = new OrderLineItem
                    {
                        ShopifyLineItemId = ExtractIdFromGid(lineItemGid),
                        ProductId = localProductId,
                        VariantId = localVariantId,
                        Sku = sku,
                        Title = liNode.GetProperty("name").GetString(),
                        Quantity = liNode.GetProperty("quantity").GetInt32()
                    };

                    lineItems.Add(lineItem);
                }
            }

            return lineItems;
        }

        /// <summary>
        /// Processes fulfillment orders and their line items from GraphQL response
        /// </summary>
        private List<FulfillmentOrder> ProcessFulfillmentOrders(JsonElement fulfillmentOrdersNode)
        {
            var fulfillmentOrders = new List<FulfillmentOrder>();

            foreach (var foNode in fulfillmentOrdersNode.EnumerateArray())
            {
                var foGid = foNode.GetProperty("id").GetString();
                var status = foNode.GetProperty("status").GetString();

                long? assignedLocationId = null;
                string assignedLocationName = null;
                if (foNode.TryGetProperty("assignedLocation", out var assignedLocNode) && assignedLocNode.ValueKind != JsonValueKind.Null)
                {
                    if (assignedLocNode.TryGetProperty("location", out var locNode) && locNode.ValueKind != JsonValueKind.Null)
                    {
                        var locGid = locNode.GetProperty("id").GetString();
                        assignedLocationId = ExtractLocationIdFromGid(locGid);
                        assignedLocationName = locNode.GetProperty("name").GetString();
                    }
                }

                var fulfillmentOrder = new FulfillmentOrder
                {
                    ShopifyFulfillmentOrderGid = foGid,
                    Status = status,
                    AssignedLocationId = assignedLocationId,
                    AssignedLocationName = assignedLocationName,
                    RawPayload = foNode.GetRawText(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Process destination
                if (foNode.TryGetProperty("destination", out var destNode) && destNode.ValueKind != JsonValueKind.Null)
                {
                    fulfillmentOrder.DestinationFirstName = destNode.TryGetProperty("firstName", out var dfn) ? dfn.GetString() : null;
                    fulfillmentOrder.DestinationLastName = destNode.TryGetProperty("lastName", out var dln) ? dln.GetString() : null;
                    fulfillmentOrder.DestinationEmail = destNode.TryGetProperty("email", out var de) ? de.GetString() : null;
                    fulfillmentOrder.DestinationPhone = destNode.TryGetProperty("phone", out var dp) ? dp.GetString() : null;
                    fulfillmentOrder.DestinationAddress1 = destNode.TryGetProperty("address1", out var da1) ? da1.GetString() : null;
                    fulfillmentOrder.DestinationAddress2 = destNode.TryGetProperty("address2", out var da2) ? da2.GetString() : null;
                    fulfillmentOrder.DestinationCity = destNode.TryGetProperty("city", out var dc) ? dc.GetString() : null;
                    fulfillmentOrder.DestinationProvince = destNode.TryGetProperty("province", out var dprov) ? dprov.GetString() : null;
                    fulfillmentOrder.DestinationCountryCode = destNode.TryGetProperty("countryCode", out var dcc) ? dcc.GetString() : null;
                    fulfillmentOrder.DestinationZip = destNode.TryGetProperty("zip", out var dz) ? dz.GetString() : null;
                }

                // Process fulfillment order line items
                var foLineItems = new List<FulfillmentOrderLineItem>();
                if (foNode.TryGetProperty("lineItems", out var foLineItemsNode))
                {
                    foreach (var foliNode in foLineItemsNode.GetProperty("nodes").EnumerateArray())
                    {
                        var foliGid = foliNode.GetProperty("id").GetString();
                        var quantity = foliNode.TryGetProperty("remainingQuantity", out var rq) ? rq.GetInt32() : 0;

                        string sku = null;
                        string name = null;
                        long orderLineItemId = 0;

                        if (foliNode.TryGetProperty("lineItem", out var liNode) && liNode.ValueKind != JsonValueKind.Null)
                        {
                            var liGid = liNode.GetProperty("id").GetString();
                            orderLineItemId = ExtractIdFromGid(liGid);
                            sku = liNode.TryGetProperty("sku", out var skuProp) ? skuProp.GetString() : null;
                            name = liNode.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                            // Fallback to variant SKU
                            if (string.IsNullOrEmpty(sku) && liNode.TryGetProperty("variant", out var vNode) && vNode.ValueKind != JsonValueKind.Null)
                            {
                                sku = vNode.TryGetProperty("sku", out var vSku) ? vSku.GetString() : null;
                            }
                        }

                        var foLineItem = new FulfillmentOrderLineItem
                        {
                            ShopifyFoLineItemGid = foliGid,
                            ShopifyOrderLineItemId = orderLineItemId,
                            Sku = sku,
                            Name = name,
                            Quantity = quantity
                        };

                        foLineItems.Add(foLineItem);
                    }
                }

                fulfillmentOrder.LineItems = foLineItems;
                fulfillmentOrders.Add(fulfillmentOrder);
            }

            return fulfillmentOrders;
        }

        /// <summary>
        /// Determines and creates order actions based on business logic:
        /// 
        /// RULE 0: If NO fulfillment orders exist → No action needed (regular in-store pickup)
        /// RULE 1: If routed to FOURNISSEUR (supplier) → Always DROP SHIP (regardless of source)
        /// RULE 2: If POS sale routed to different location (not supplier) → TRANSFER
        /// RULE 3: If Web sale routed to FOURNISSEUR → DROP SHIP
        /// 
        /// Priority: Check fulfillment orders exist, then supplier location, then POS routing
        /// </summary>
        private async Task<List<OrderActionDTO>> DetermineAndCreateOrderActions(
            Order order, 
            JsonElement fulfillmentOrdersNode, 
            long? posLocationId, 
            string sourceName,
            HashSet<string> supplierLocationNames)
        {
            var actionsCreated = new List<OrderActionDTO>();
            var isPosSource = sourceName?.Equals("pos", StringComparison.OrdinalIgnoreCase) == true;

            // RULE 0: If no fulfillment orders, it's a regular in-store pickup - no action needed
            if (fulfillmentOrdersNode.ValueKind == JsonValueKind.Null || fulfillmentOrdersNode.GetArrayLength() == 0)
            {
                _logger.LogInformation("[OrderService] No fulfillment orders found - Regular in-store pickup, no action needed");
                return actionsCreated;
            }

            foreach (var foNode in fulfillmentOrdersNode.EnumerateArray())
            {
                var foGid = foNode.GetProperty("id").GetString();
                var foId = order.FulfillmentOrders.FirstOrDefault(f => f.ShopifyFulfillmentOrderGid == foGid)?.Id;

                if (!foId.HasValue) continue;

                long? assignedLocationId = null;
                string assignedLocationName = null;

                if (foNode.TryGetProperty("assignedLocation", out var assignedLocNode) && assignedLocNode.ValueKind != JsonValueKind.Null)
                {
                    if (assignedLocNode.TryGetProperty("location", out var locNode) && locNode.ValueKind != JsonValueKind.Null)
                    {
                        var locGid = locNode.GetProperty("id").GetString();
                        assignedLocationId = ExtractLocationIdFromGid(locGid);
                        assignedLocationName = locNode.GetProperty("name").GetString();
                    }
                }

                if (!assignedLocationId.HasValue)
                {
                    _logger.LogDebug("[OrderService] No assigned location for FO {FoGid}, skipping action creation", foGid);
                    continue;
                }

                // Check if assigned location is a supplier (case-insensitive)
                var assignedLocationNameUpper = assignedLocationName?.ToUpperInvariant() ?? "";
                var isSupplierLocation = supplierLocationNames.Contains(assignedLocationNameUpper);

                _logger.LogDebug("[OrderService] Checking FO {FoGid}: AssignedLocation='{AssignedLocation}' (Upper='{AssignedLocationUpper}'), IsSupplier={IsSupplier}, SupplierNames=[{SupplierNames}]", 
                    foGid, assignedLocationName, assignedLocationNameUpper, isSupplierLocation, string.Join(", ", supplierLocationNames));

                string actionType = null;
                string reason = null;
                long? fromLocationId = null;

                // RULE 1: If routed to FOURNISSEUR → Always DROP SHIP
                if (isSupplierLocation)
                {
                    actionType = "DROP_SHIP";
                    if (isPosSource && posLocationId.HasValue)
                    {
                        reason = $"POS sale at location {posLocationId} routed to supplier {assignedLocationName} ({assignedLocationId})";
                        fromLocationId = posLocationId;
                    }
                    else
                    {
                        reason = $"Web sale routed to supplier {assignedLocationName} ({assignedLocationId})";
                        fromLocationId = null; // Web orders don't have a "from" location
                    }
                    _logger.LogInformation("[OrderService] Action: DROP SHIP - Order routed to supplier {SupplierName}", assignedLocationName);
                }
                // RULE 2: If POS sale routed to different location (not supplier) → TRANSFER
                else if (isPosSource && posLocationId.HasValue && posLocationId.Value != assignedLocationId.Value)
                {
                    actionType = "TRANSFER";
                    reason = $"POS sale at location {posLocationId} needs transfer to location {assignedLocationName} ({assignedLocationId})";
                    fromLocationId = posLocationId;
                    _logger.LogInformation("[OrderService] Action: TRANSFER - POS order from {FromLocation} to {ToLocation}", posLocationId, assignedLocationId);
                }
                else
                {
                    // No action needed (e.g., POS sale fulfilled from same location, or web sale to regular location)
                    _logger.LogDebug("[OrderService] No action needed for FO {FoGid} - Source: {Source}, POS Location: {PosLoc}, Assigned: {AssignedLoc}", 
                        foGid, sourceName, posLocationId, assignedLocationId);
                    continue;
                }

                // Create the action
                var orderAction = new OrderAction
                {
                    OrderId = order.Id,
                    FulfillmentOrderId = foId.Value,
                    ActionType = actionType,
                    Reason = reason,
                    FromLocationId = fromLocationId,
                    ToLocationId = assignedLocationId,
                    Payload = foNode.GetRawText(),
                    IdempotencyKey = $"{order.ShopifyOrderId}:{foGid}:{actionType}",
                    CreatedAt = DateTime.UtcNow
                };

                var createdAction = await _orderRepository.CreateOrderActionAsync(orderAction);

                actionsCreated.Add(new OrderActionDTO
                {
                    Id = createdAction.Id,
                    OrderId = createdAction.OrderId,
                    FulfillmentOrderId = createdAction.FulfillmentOrderId,
                    ActionType = createdAction.ActionType,
                    Reason = createdAction.Reason,
                    FromLocationId = createdAction.FromLocationId,
                    ToLocationId = createdAction.ToLocationId,
                    Payload = createdAction.Payload,
                    IdempotencyKey = createdAction.IdempotencyKey,
                    CreatedAt = createdAction.CreatedAt
                });
            }

            return actionsCreated;
        }

        /// <summary>
        /// Enriches order data with product, variant, tags, and supplier information and publishes to Prediko queue
        /// </summary>
        private async Task PublishOrderToPredikoInternalAsync(
            ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO.OrderDTO orderDto,
            ISupplierRepository supplierRepo,
            IProductRepository productRepo,
            IShopifyRepository shopifyRepo,
            IPredikoOrderRMQService predikoRMQ)
        {
            _logger.LogInformation("[OrderService] Enriching order {OrderId} for Prediko publication", orderDto.Id);

            try
            {
                // Create a lookup dictionary for enrichment data by ShopifyOrderLineItemId
                var enrichmentLookup = new Dictionary<long, (ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoProductInfoDTO? Product, 
                                                              ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoVariantInfoDTO? Variant)>();

                // First, gather enrichment data from OrderLineItems
                foreach (var lineItem in orderDto.LineItems)
                {
                    ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoProductInfoDTO? productInfo = null;
                    ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoVariantInfoDTO? variantInfo = null;

                    // Manually fetch and enrich with variant data
                    if (lineItem.VariantId.HasValue)
                    {
                        try
                        {
                            // Fetch variant with OEM using ShopifyRepository
                            var variant = await shopifyRepo.GetVariantByIdAsync((int)lineItem.VariantId.Value);
                            if (variant != null)
                            {
                                var variantDto = new ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoVariantInfoDTO
                                {
                                    VariantId = variant.Id,
                                    ShopifyId = variant.ShopifyId,
                                    Title = variant.Title,
                                    SKU = variant.SKU,
                                    Price = variant.Price,
                                    Barcode = variant.Barcode,
                                    Weight = variant.Weight,
                                    WeightUnit = variant.WeightUnit,
                                    OEMId = variant.OEMId,
                                    OEMName = variant.OEM?.Name
                                };

                                // Extract supplier code from SKU (format: XX-1234567...)
                                if (!string.IsNullOrWhiteSpace(variant.SKU))
                                {
                                    var skuParts = variant.SKU.Split('-');
                                    if (skuParts.Length > 0)
                                    {
                                        var supplierCode = skuParts[0].Trim();
                                        
                                        try
                                        {
                                            var supplier = await supplierRepo.GetSupplierByCodeAsync(supplierCode);
                                            if (supplier != null)
                                            {
                                                variantDto.Suppliers.Add(new ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoSupplierInfoDTO
                                                {
                                                    SupplierId = (int)supplier.SupplierId,
                                                    SupplierName = supplier.Name,
                                                    SupplierCode = supplier.Code,
                                                    PredikoId = supplier.SupplierPredikoId
                                                });

                                                _logger.LogDebug("[OrderService] Found supplier {SupplierName} (Code: {SupplierCode}, PredikoId: {PredikoId}) for variant SKU: {SKU}",
                                                    supplier.Name, supplier.Code, supplier.SupplierPredikoId, variant.SKU);
                                            }
                                            else
                                            {
                                                _logger.LogDebug("[OrderService] No supplier found for code '{SupplierCode}' extracted from SKU: {SKU}",
                                                    supplierCode, variant.SKU);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "[OrderService] Error looking up supplier for code '{SupplierCode}' from SKU: {SKU}",
                                                supplierCode, variant.SKU);
                                        }
                                    }
                                }

                                variantInfo = variantDto;
                                _logger.LogDebug("[OrderService] Enriched line item {LineItemId} with variant {VariantId}",
                                    lineItem.Id, variant.Id);
                            }
                            else
                            {
                                _logger.LogWarning("[OrderService] Variant {VariantId} not found for line item {LineItemId}",
                                    lineItem.VariantId, lineItem.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[OrderService] Error fetching variant {VariantId} for line item {LineItemId}: {Message}",
                                lineItem.VariantId, lineItem.Id, ex.Message);
                        }
                    }

                    // Manually fetch and enrich with product data
                    if (lineItem.ProductId.HasValue)
                    {
                        try
                        {
                            // Fetch product with tags, vendor, type using ProductRepository
                            var product = await productRepo.GetProductByIdAsync((int)lineItem.ProductId.Value);
                            if (product != null)
                            {
                                var productTags = product.ProductTags?
                                    .Select(pt => pt.Tag?.Title)
                                    .Where(t => !string.IsNullOrEmpty(t))
                                    .ToList() ?? new List<string>();
                                
                                productInfo = new ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoProductInfoDTO
                                {
                                    ProductId = product.Id,
                                    ShopifyId = product.ShopifyId,
                                    Title = product.Title,
                                    Vendor = product.Vendor?.Title,
                                    ProductType = product.ProductType?.Name,
                                    Tags = productTags
                                };

                                _logger.LogDebug("[OrderService] Enriched line item {LineItemId} with product {ProductId}",
                                    lineItem.Id, product.Id);
                            }
                            else
                            {
                                _logger.LogWarning("[OrderService] Product {ProductId} not found for line item {LineItemId}",
                                    lineItem.ProductId, lineItem.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[OrderService] Error fetching product {ProductId} for line item {LineItemId}: {Message}",
                                lineItem.ProductId, lineItem.Id, ex.Message);
                        }
                    }

                    // Store enrichment data in lookup dictionary
                    enrichmentLookup[lineItem.ShopifyLineItemId] = (productInfo, variantInfo);
                }

                // Now enrich all FulfillmentOrderLineItems with the gathered data
                foreach (var fulfillmentOrder in orderDto.FulfillmentOrders ?? new List<ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO.FulfillmentOrderDTO>())
                {
                    foreach (var foLineItem in fulfillmentOrder.LineItems ?? new List<ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO.FulfillmentOrderLineItemDTO>())
                    {
                        if (enrichmentLookup.TryGetValue(foLineItem.ShopifyOrderLineItemId, out var enrichmentData))
                        {
                            foLineItem.Product = enrichmentData.Product;
                            foLineItem.Variant = enrichmentData.Variant;
                            
                            _logger.LogDebug("[OrderService] Enriched FO line item {FoLineItemId} with product and variant data",
                                foLineItem.Id);
                        }
                        else
                        {
                            _logger.LogDebug("[OrderService] No enrichment data found for FO line item {FoLineItemId} (ShopifyOrderLineItemId: {ShopifyOrderLineItemId})",
                                foLineItem.Id, foLineItem.ShopifyOrderLineItemId);
                        }
                    }
                }

                // Create Prediko message (no longer using EnrichedLineItems)
                var predikoMessage = new ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO.PredikoOrderMessageDTO
                {
                    Order = orderDto,
                    MessageCreatedAt = DateTime.UtcNow,
                    SourceName = orderDto.SourceName,
                    SaleLocationId = orderDto.SaleLocationId,
                    SaleLocationName = orderDto.SaleLocationName
                };

                // Publish to RabbitMQ
                var published = await predikoRMQ.PublishOrderAsync(predikoMessage);

                if (published)
                {
                    var totalEnrichedItems = orderDto.FulfillmentOrders?.Sum(fo => fo.LineItems?.Count ?? 0) ?? 0;
                    _logger.LogInformation("[OrderService] ✅ Successfully published order {OrderId} to Prediko queue with {FulfillmentOrderCount} fulfillment orders and {LineItemCount} enriched line items",
                        orderDto.Id, orderDto.FulfillmentOrders?.Count ?? 0, totalEnrichedItems);
                }
                else
                {
                    _logger.LogWarning("[OrderService] ⚠️ Failed to publish order {OrderId} to Prediko queue", orderDto.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderService] Error publishing order {OrderId} to Prediko: {Message}",
                    orderDto.Id, ex.Message);
                throw;
            }
        }

        #endregion
    }
}
