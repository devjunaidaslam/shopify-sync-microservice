using Amazon.Runtime;
using AWS.Logger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Infrastructure.Job.Background;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Implementation;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ;
using PartFinderMicroServices_DataAccessLayer.Model;
using Quartz;
using ShopifyService_API.Middleware;
using System.Text;
using System.Text.Json.Serialization;

namespace ShopifyService_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContextFactory<PartFinderDbContext>(opt =>
                opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContext<PartFinderDbContext>(opt =>
                opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsql =>
                {
                    npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));

            builder.Services.Configure<ShopifySetting>(
                builder.Configuration.GetSection("Shopify"));

            builder.Services.Configure<RabbitMQSetting>(
                builder.Configuration.GetSection("RabbitMQ"));

            builder.Services.AddSingleton<PageCursorTracker>();
            builder.Services.AddScoped<IShopifyService, ShopifyService>();
            builder.Services.AddScoped<IShopifyUpdateService, ShopifyUpdateService>();
            builder.Services.AddScoped<IFitmentService, FitmentService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();

            // Order stack must register before WebHookService (constructor dependency).
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPredikoOrderRMQService, PredikoOrderRMQService>();

            builder.Services.AddScoped<IWebHookService, WebHookService>();
            builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
            builder.Services.AddScoped<ICollectionService, CollectionService>();
            builder.Services.AddScoped<IVendorRepository, VendorRepository>();
            builder.Services.AddScoped<IVendorService, VendorService>();
            builder.Services.AddScoped<ITagRepository, TagRepository>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<ILocationRepository, LocationRepository>();
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
            builder.Services.AddScoped<IWebHookRMQService, WebHookRMQService>();
            builder.Services.AddScoped<IShopifyUpdateRMQService, ShopifyUpdateRMQService>();
            builder.Services.AddScoped<IShopifyRepository, ShopifyRepository>();
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
            builder.Services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();

            builder.Services.AddSingleton<ImportProductsBackgroundService>();
            builder.Services.AddHostedService(provider =>
                provider.GetRequiredService<ImportProductsBackgroundService>());

            builder.Services.AddQuartz(q =>
            {
                var jobKey = new JobKey("ImportProductsQuartzJob");
                q.AddJob<ImportProductsQuartzJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ImportProductsQuartzJob-trigger")
                    .WithCronSchedule(
                        builder.Configuration["Quartz:ImportShopifyJob:Cron"]
                        ?? "0 0/10 * * * ?"));
            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            #region JWT Authentication

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(
                jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing"));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SwaggerPolicy", policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });
            });

            #endregion

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Shopify Sync API",
                    Version = "v1"
                });

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Enter a JWT bearer token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

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

            app.UsePathBase("/shopify");
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/shopify/swagger/v1/swagger.json", "Shopify API V1");
                c.RoutePrefix = "swagger";
            });

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
