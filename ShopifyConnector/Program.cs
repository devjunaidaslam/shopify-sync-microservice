using Amazon.Runtime;
using AWS.Logger;
using Microsoft.EntityFrameworkCore;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ;
using PartFinderMicroServices_DataAccessLayer.Model;
using ShopifyConnector.BackgroundJob;
using ShopifyConnector.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ShopifySetting>(
                 builder.Configuration.GetSection("Shopify"));

builder.Services.Configure<RabbitMQSetting>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddHostedService<WebHookRMQJob>();
builder.Services.AddHostedService<ShopifyUpdateRMQJob>();
// Separate services for inbound and outbound queue processing
builder.Services.AddScoped<IWebHookRMQService, WebHookRMQService>();
builder.Services.AddScoped<IShopifyUpdateRMQService, ShopifyUpdateRMQService>();

// Register Order services BEFORE WebHookService (dependency requirement)
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Register Prediko Order RMQ publisher service
builder.Services.AddScoped<IPredikoOrderRMQService, PredikoOrderRMQService>();

builder.Services.AddScoped<IWebHookService, WebHookService>();
builder.Services.AddScoped<IShopifyUpdateService, ShopifyUpdateService>();
builder.Services.AddScoped<IShopifyService, ShopifyService>();
builder.Services.AddScoped<IFitmentService, FitmentService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddTransient<IShopifyRepository, ShopifyRepository>();
builder.Services.AddDbContextFactory<PartFinderDbContext>(opt =>
   opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<PartFinderDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), builder =>
    {
        builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));
builder.Services.AddSingleton<PageCursorTracker>();
builder.Services.AddTransient<ICommonService, CommonService>();
builder.Services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
builder.Services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var awsAccessKey = builder.Configuration["AppSettings:AWS_ACCESS_KEY"];
var awsSecretKey = builder.Configuration["AppSettings:AWS_SECRET_ACCESS_KEY"];
if (!string.IsNullOrWhiteSpace(awsAccessKey) &&
    !string.IsNullOrWhiteSpace(awsSecretKey) &&
    !awsAccessKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
{
    var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
    var awsLoggerConfig = new AWSLoggerConfig(builder.Configuration["AWS:LogGroup"])
    {
        Region = builder.Configuration["AWS:Region"],
        Credentials = awsCredentials
    };
    builder.Logging.AddAWSProvider(awsLoggerConfig);
}

var app = builder.Build();
app.UsePathBase("/shopifyconnector"); // 👈 this makes sure all paths are aware of the base path
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/shopifyconnector/swagger/v1/swagger.json", "Shopify API V1");
    c.RoutePrefix = "swagger";
});
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
