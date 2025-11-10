using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace arroyoSeco.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Carga configuración (appsettings.* del proyecto API si se ejecuta desde raíz)
        var basePath = Directory.GetCurrentDirectory();
        var builderConfig = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builderConfig.Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=arroyoSeco;User=root;Password=;";
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
        return new AppDbContext(optionsBuilder.Options);
    }
}