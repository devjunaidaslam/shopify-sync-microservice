using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PartFinder_DataAccess.Context;

namespace PartFinderMicroServices_DataAccessLayer.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PartFinderDbContext>
    {
        public PartFinderDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../Services/AuthService/src/AuthService_API");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                                                .SetBasePath(basePath)
                                                .AddJsonFile("appsettings.json")
                                                .Build();

            var builder = new DbContextOptionsBuilder<PartFinderDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseNpgsql(connectionString);

            return new PartFinderDbContext(builder.Options);
        }
    }
}
