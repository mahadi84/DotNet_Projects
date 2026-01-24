using CBS.Infrastructure.Migration;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System.Reflection;

namespace CBS.Infrastructure.AddToRoute;

public static class DependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // মাইগ্রেশন রান করা
        DatabaseMigrator.Migrate(connectionString!);

        // অন্যান্য সার্ভিস (Dapper, Repositories)
        services.AddMySqlDataSource(connectionString!);

        //services.AddScoped<IBranchRepository, BranchRepository>();

        return services;
    }

}