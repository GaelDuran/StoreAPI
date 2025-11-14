using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StoreAPI;
using StoreAPI.Controllers;
using StoreAPI.Models.Entities;
using Xunit;

namespace StoreAPI.Tests
{
    public class InvoicesControllerTests
    {
        private StoreDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StoreDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new StoreDbContext(options);
        }

        private IHttpClientFactory CreateHttpClientFactoryReturning(string responseContent)
        {
            var handler = new DelegatingHandlerStub((request, ct) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            });
            var client = new HttpClient(handler);
            return new SimpleHttpClientFactory(client);
        }

        [Fact]
        public async Task Analyze_Returns_ParsedJson_When_OpenAI_Returns_JsonContent()
        {
            // Arrange
            var ctx = CreateInMemoryContext("TestDb1");
            // seed invoices
            ctx.Invoice.Add(new Invoice { Id = 1, OrderId = 1, InvoiceNumber = "I1", IssueDate = DateTime.UtcNow, Subtotal = 100, Tax = 16, Total = 116, Currency = "MXN", IsPaid = true, BillingName = "A", CreatedAt = DateTime.UtcNow });
            ctx.Invoice.Add(new Invoice { Id = 2, OrderId = 1, InvoiceNumber = "I2", IssueDate = DateTime.UtcNow, Subtotal = 200, Tax = 32, Total = 232, Currency = "USD", IsPaid = false, BillingName = "B", CreatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            // prepare fake OpenAI response
            var aiJson = JsonSerializer.Serialize(new
            {
                totalInvoices = 2,
                paidInvoices = 1,
                unpaidInvoices = 1,
                totalRevenue = 348.0,
                averageInvoiceAmount = 174.0,
                commonCurrencies = new[] { "MXN", "USD" },
                patterns = new[] { "50% de facturas están pagadas", "MXN es la moneda más usada" }
            });

            var chatResponse = JsonSerializer.Serialize(new
            {
                id = "test",
                choices = new[] { new { message = new { content = aiJson } } }
            });

            var httpFactory = CreateHttpClientFactoryReturning(chatResponse);

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "OpenAI:ApiKey", "test" }
            }).Build();

            var controller = new InvoicesController(ctx, configuration, httpFactory);

            // Act
            var result = await controller.Analyze(null, null);

            // Assert
            Assert.IsType<JsonResult>(result);
            var jsonResult = (JsonResult)result;
            // jsonResult.Value is a JsonElement (root)
            var root = (JsonElement)jsonResult.Value;
            Assert.Equal(2, root.GetProperty("totalInvoices").GetInt32());
            Assert.Equal(1, root.GetProperty("paidInvoices").GetInt32());
            Assert.Equal(1, root.GetProperty("unpaidInvoices").GetInt32());
            Assert.Equal(348.0, root.GetProperty("totalRevenue").GetDouble());
        }

        // Helper classes
        private class SimpleHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public SimpleHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private class DelegatingHandlerStub : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;
            public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
            {
                _handlerFunc = handlerFunc;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handlerFunc(request, cancellationToken);
            }
        }
    }
}

