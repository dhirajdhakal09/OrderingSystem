using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using OrderService.Data;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(
                 Order order)
        {
            var createdOrder =
               await _orderService.CreateOrderAsync(order);

            return CreatedAtAction(
                null,
                new { id = createdOrder.Id },
                createdOrder);
        }
    }
}
