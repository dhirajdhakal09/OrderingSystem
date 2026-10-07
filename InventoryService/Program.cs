using Humanizer;
using InventoryService.Data;
using InventoryService.Messaging;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// Bind this service to a fixed HTTP port so it can run independently from others.
builder.WebHost.UseUrls("http://localhost:5003");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

var inventoryConnection = builder.Configuration.GetConnectionString("InventoryDb")
                          ?? Environment.GetEnvironmentVariable("ConnectionStrings__InventoryDb");

builder.Services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(inventoryConnection));

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();

builder.Services.AddHostedService<RabbitMQConsumerWorker>();
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();
app.MapControllers();
app.Run();



//• To see the visual interactive playground (Scalar UI):
//👉 http://localhost:5003/scalar/v1
//• To see the raw JSON specification data:
//👉 http://localhost:5003/openapi/v1.json
