using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jabasoft.Ai.Data;

/// <summary>
/// Creates <see cref="AiDbContext"/> for design-time EF Core tooling.
/// Reads the connection string from JABASOFT_AI_CONNECTION_STRING when
/// set (matches ConnectionStrings:JabasoftAi in Jabasoft.Broker's
/// appsettings.json), otherwise falls back to the default local SQL
/// Server instance - same pattern as Stylebook.Data's
/// StylebookDbContextFactory.
/// </summary>
public sealed class AiDbContextFactory : IDesignTimeDbContextFactory<AiDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "JABASOFT_AI_CONNECTION_STRING";

    private const string DefaultConnectionString =
        "Server=localhost;Database=JabasoftAi;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

    public AiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DefaultConnectionString;
        }

        var options = new DbContextOptionsBuilder<AiDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AiDbContext(options);
    }
}
