using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreAPI.Models.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace StoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly StoreDbContext _context;
        public InvoicesController(StoreDbContext context)
        {
            _context = context;
        }

        // GET: api/invoices?orderId=1&isPaid=true
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? orderId, [FromQuery] bool? isPaid)
        {
            var query = _context.Invoice.AsQueryable();
            if (orderId.HasValue)
                query = query.Where(i => i.OrderId == orderId);
            if (isPaid.HasValue)
                query = query.Where(i => i.IsPaid == isPaid);
            var result = await query.ToListAsync();
            return Ok(result);
        }

        // GET: api/invoices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _context.Invoice.FindAsync(id);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        // POST: api/invoices
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar unicidad de InvoiceNumber
            if (await _context.Invoice.AnyAsync(i => i.InvoiceNumber == invoice.InvoiceNumber))
                return Conflict("InvoiceNumber ya existe.");

            // Validar existencia de Order
            var order = await _context.Order.FindAsync(invoice.OrderId);
            if (order == null)
                return BadRequest("OrderId no existe.");

            // Calcular Total si no viene
            if (invoice.Total == 0)
                invoice.Total = invoice.Subtotal + invoice.Tax;

            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = null;

            _context.Invoice.Add(invoice);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
    }
}

