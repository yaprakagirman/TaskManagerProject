namespace TaskManager.API.Configuration;

public static class DevelopmentEnvironmentConfiguration
{
    private const string PlaceholderUser = "YOUR_POSTGRES_USER";

    public static void AddLocalEnvironmentFile(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() ||
            !ContainsPlaceholder(builder.Configuration.GetConnectionString("DefaultConnection")))
        {
            return;
        }

        var values = ReadEnvironmentFile(FindEnvironmentFile(builder.Environment.ContentRootPath));
        if (!values.TryGetValue("POSTGRES_USER", out var username) ||
            !values.TryGetValue("POSTGRES_PASSWORD", out var password) ||
            !values.TryGetValue("POSTGRES_DB", out var database))
        {
            return;
        }

        var host = values.GetValueOrDefault("POSTGRES_HOST", "localhost");
        var port = values.GetValueOrDefault("POSTGRES_PORT", "5432");
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                $"Host={host};Port={port};Database={database};Username={username};Password={password}"
        };

        if (values.TryGetValue("JWT_SECRET", out var jwtSecret))
        {
            overrides["Jwt:Key"] = jwtSecret;
        }

        builder.Configuration.AddInMemoryCollection(overrides);
    }

    private static bool ContainsPlaceholder(string? connectionString) =>
        string.IsNullOrWhiteSpace(connectionString) ||
        connectionString.Contains(PlaceholderUser, StringComparison.Ordinal);

    private static string? FindEnvironmentFile(string contentRootPath)
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(contentRootPath, ".env"),
            Path.GetFullPath(Path.Combine(contentRootPath, "..", ".env"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Dictionary<string, string> ReadEnvironmentFile(string? path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (path is null)
        {
            return values;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            values[line[..separatorIndex].Trim()] =
                line[(separatorIndex + 1)..].Trim().Trim('"');
        }

        return values;
    }
}
