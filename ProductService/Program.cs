using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
// Bind this service to a fixed HTTP port so it can run independently from others.
builder.WebHost.UseUrls("http://localhost:5002");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

// Configure EF Core DbContext - connection comes from configuration (appsettings or environment)
var productConnection = builder.Configuration.GetConnectionString("ProductDb")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__ProductDb");
builder.Services.AddDbContext<ProductDbContext>(options => options.UseSqlServer(productConnection));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Redis");

    return ConnectionMultiplexer.Connect(connectionString!);
});

builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Apply migrations and seed data
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
//    db.Database.Migrate();

//    // Seed sample products
//    //if (!db.Products.Any())
//    //{
//    //    db.Products.AddRange(
//    //        new Product { Name = "Widget", Price = 9.99 },
//    //        new Product { Name = "Gadget", Price = 19.99 },
//    //        new Product { Name = "Thingamajig", Price = 14.49 }
//    //    );
//    //    db.SaveChanges();
//    //}
//}
app.MapControllers();
app.Run();
