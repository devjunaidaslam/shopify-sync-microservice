
using Amazon.Runtime;
using AWS.Logger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
                opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), builder =>
                {
                    builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));


            builder.Services.Configure<ShopifySetting>(
                  builder.Configuration.GetSection("Shopify"));

            builder.Services.Configure<RabbitMQSetting>(builder.Configuration.GetSection("RabbitMQ"));

            builder.Services.AddSingleton<PageCursorTracker>();
            builder.Services.AddScoped<IShopifyService, ShopifyService>();
            builder.Services.AddScoped<IShopifyUpdateService, ShopifyUpdateService>();
            builder.Services.AddScoped<IFitmentService, FitmentService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            
            // Register Order services BEFORE WebHookService (dependency requirement)
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
            // Note: IProductRepository already registered above at line 51
            builder.Services.AddScoped<IOrderService, OrderService>();
            
            // Register Prediko Order RMQ publisher service
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
            builder.Services.AddSingleton<ImportProductsBackgroundService>();
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
            builder.Services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<ImportProductsBackgroundService>());
           // builder.Services.AddHostedService<ShopifyRetryWorker>();
            // Quartz job registration

            builder.Services.AddQuartz(q =>
            {
                var jobKey = new JobKey("ImportProductsQuartzJob");
                q.AddJob<ImportProductsQuartzJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ImportProductsQuartzJob-trigger")
                    .WithCronSchedule(builder.Configuration["Quartz:ImportShopifyJob:Cron"]));

            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });


            #region JWT Authentication

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing"));
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

            builder.Services.AddAuthorization();

            #endregion

            #region JWT Authorization - 2

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SwaggerPolicy", policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Part Finder API's", Version = "v1" });

                //var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //option.IncludeXmlComments(xmlPath);

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
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
            new string[]{}
        }
    });
            });

            #endregion
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            app.UsePathBase("/shopify"); // 👈 this makes sure all paths are aware of the base path
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/shopify/swagger/v1/swagger.json", "Shopify API V1");
                c.RoutePrefix = "swagger";
            });
           
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
