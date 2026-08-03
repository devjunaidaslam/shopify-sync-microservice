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

        public OrderController(IOrderService orderService, ICommonService commonService)
        {
            _orderService = orderService;
            _commonService = commonService;
        }

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
