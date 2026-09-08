using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopifySync_DataAccess.Context;

namespace ShopifySync_DataAccessLayer.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopifySyncDbContext>
    {
        public ShopifySyncDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../Services/AuthService/src/AuthService_API");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                                                .SetBasePath(basePath)
                                                .AddJsonFile("appsettings.json")
                                                .Build();

            var builder = new DbContextOptionsBuilder<ShopifySyncDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseNpgsql(connectionString);

            return new ShopifySyncDbContext(builder.Options);
        }
    }
}
