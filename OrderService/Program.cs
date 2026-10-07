using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Messaging;
using Scalar.AspNetCore;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);
// Bind this service to a fixed HTTP port so it can run independently from others.
builder.WebHost.UseUrls("http://localhost:5004");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

var orderConnection = builder.Configuration.GetConnectionString("OrderDb")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__OrderDb");
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlServer(orderConnection));

builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

 

app.MapControllers();
app.Run();

