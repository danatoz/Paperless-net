using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Paperless.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("PAPERLESS_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=paperless;Username=paperless;Password=paperless";

        optionsBuilder.UseNpgsql(connectionString);

        var dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = "PostgreSQL",
            ConnectionString = connectionString
        });

        return new AppDbContext(optionsBuilder.Options, dbOptions);
    }
}
