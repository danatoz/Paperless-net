using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Persistence.Interceptors;

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

        // Design-time interceptors: SoftDeleteInterceptor has no dependencies;
        // AuditInterceptor gracefully handles missing IHttpContextAccessor.
        var httpContextAccessor = new HttpContextAccessor();
        var softDeleteInterceptor = new SoftDeleteInterceptor();
        var auditInterceptor = new AuditInterceptor(httpContextAccessor);

        return new AppDbContext(
            optionsBuilder.Options,
            dbOptions,
            softDeleteInterceptor,
            auditInterceptor);
    }
}
