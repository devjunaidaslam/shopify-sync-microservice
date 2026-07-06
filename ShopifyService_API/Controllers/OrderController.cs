using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO;

namespace ShopifyService_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICommonService _commonService;
        private readonly ResponseMessageList _apiResponseMessageList = new ResponseMessageList();

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderController"/> class.
        /// </summary>
        /// <param name="orderService">Service for order operations.</param>
        /// <param name="commonService">Service for logging and common operations.</param>
        public OrderController(IOrderService orderService, ICommonService commonService)
        {
            _orderService = orderService;
            _commonService = commonService;
        }

        /// <summary>
        /// Retrieves all orders with optional filtering and pagination.
        /// </summary>
        /// <param name="filter">Filter and pagination options for orders.</param>
        /// <returns>A paginated list of orders or an error response.</returns>
        [HttpGet]
        public async Task<ActionResult<Response>> GetAllOrders([FromQuery] OrderFilterDTO filter)
        {
            try
            {
                var response = await _orderService.GetAllOrdersPaginatedAsync(filter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetAllOrders", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Retrieves an order by its unique identifier.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <returns>The order details or an error response.</returns>
        [HttpGet("{orderId}")]
        public async Task<ActionResult<Response>> GetOrderById(long orderId)
        {
            try
            {
                var response = await _orderService.GetOrderByIdAsync(orderId);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderById", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Retrieves an order by its Shopify Order ID.
        /// </summary>
        /// <param name="shopifyOrderId">The Shopify Order ID.</param>
        /// <returns>The order details or an error response.</returns>
        [HttpGet("shopify/{shopifyOrderId}")]
        public async Task<ActionResult<Response>> GetOrderByShopifyOrderId(long shopifyOrderId)
        {
            try
            {
                var response = await _orderService.GetOrderByShopifyOrderIdAsync(shopifyOrderId);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderByShopifyOrderId", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="orderCreateDto">The order data to create.</param>
        /// <returns>The created order or an error response.</returns>
        [HttpPost]
        public async Task<ActionResult<Response>> CreateOrder([FromBody] OrderCreateDTO orderCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.BadRequest("Invalid order data"));
                }
                
                var response = await _orderService.CreateOrderAsync(orderCreateDto);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return CreatedAtAction(nameof(GetOrderById), new { orderId = ((dynamic)response.Data).Id }, response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateOrder", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Updates an existing order.
        /// </summary>
        /// <param name="orderUpdateDto">The order data to update.</param>
        /// <returns>The updated order or an error response.</returns>
        [HttpPut]
        public async Task<ActionResult<Response>> UpdateOrder([FromBody] OrderUpdateDTO orderUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.BadRequest("Invalid order data"));
                }
                
                var response = await _orderService.UpdateOrderAsync(orderUpdateDto);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "UpdateOrder", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Deletes an order by its unique identifier.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to delete.</param>
        /// <returns>Success response or an error response.</returns>
        [HttpDelete("{orderId}")]
        public async Task<ActionResult<Response>> DeleteOrder(long orderId)
        {
            try
            {
                var response = await _orderService.DeleteOrderAsync(orderId);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "DeleteOrder", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Creates a new order action.
        /// </summary>
        /// <param name="orderActionCreateDto">The order action data to create.</param>
        /// <returns>The created order action or an error response.</returns>
        [HttpPost("action")]
        public async Task<ActionResult<Response>> CreateOrderAction([FromBody] OrderActionCreateDTO orderActionCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.BadRequest("Invalid order action data"));
                }
                
                var response = await _orderService.CreateOrderActionAsync(orderActionCreateDto);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "CreateOrderAction", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Retrieves all order actions for a specific order.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <returns>A list of order actions or an error response.</returns>
        [HttpGet("{orderId}/actions")]
        public async Task<ActionResult<Response>> GetOrderActions(long orderId)
        {
            try
            {
                var response = await _orderService.GetOrderActionsByOrderIdAsync(orderId);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "GetOrderActions", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }

        /// <summary>
        /// Imports an order from Shopify by its numeric order ID.
        /// Fetches order data via GraphQL, creates Order, Customer, OrderLineItem, FulfillmentOrder, 
        /// FulfillmentOrderLineItem entities, and determines business actions (Transfer/Drop Ship).
        /// </summary>
        /// <param name="request">The import request containing Shopify order ID and optional supplier location names</param>
        /// <returns>The imported order with all related entities and created actions</returns>
        [HttpPost("import-from-shopify")]
        public async Task<ActionResult<Response>> ImportOrderFromShopify([FromBody] ShopifyOrderImportRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.BadRequest("Invalid import request data"));
                }
                
                var response = await _orderService.ImportOrderFromShopifyAsync(request);
                
                if (!response.IsSuccess)
                {
                    if (response.StatusCode == StatusCodes.Status404NotFound)
                        return NotFound(response);
                    if (response.StatusCode == StatusCodes.Status400BadRequest)
                        return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _commonService.ErrorLogs(ex.StackTrace ?? "No stack trace available", "ImportOrderFromShopify", 1, ex.Message, ex.ToString());
                return BadRequest(ResponseHelper.BadRequest(_apiResponseMessageList.FailResponseMessage));
            }
        }
    }
}
