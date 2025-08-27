using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StoreAPI.Models.DTOs;
using StoreAPI.Models.Entities;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly StoreDbContext _context;
        public OrderController(StoreDbContext context)
        {
            _context = context;
        }
        
        [HttpPost]
        public async Task<ActionResult> CreateOrder(
            [FromBody] OrderCDTO order
            )
        {
            var newOrder = new Order()
            {
                SystemUserId = order.SystemUserId,
                CreatedAt = DateTime.Now,
                Total = order.Total,
            };
            _context.Order.Add(newOrder);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
