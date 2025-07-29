using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Configuration;

namespace phoenix_sangam_api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddGenericDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConfig = new DatabaseConfig();
        configuration.GetSection("Database").Bind(databaseConfig);
        
        services.Configure<DatabaseConfig>(configuration.GetSection("Database"));
        
        services.AddDbContext<UserDbContext>(options =>
        {
            var connectionString = databaseConfig.GetConnectionString();
            
            switch (databaseConfig.Provider?.ToLower())
            {
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;
                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "sqlserver":
                default:
                    options.UseSqlServer(connectionString);
                    break;
            }
        });
        
        return services;
    }
} 