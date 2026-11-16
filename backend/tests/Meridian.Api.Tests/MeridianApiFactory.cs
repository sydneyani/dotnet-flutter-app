using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Meridian.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meridian.Api.Tests;

public sealed class MeridianApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"meridian-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["Database:ConnectionString"] = $"Data Source={_databasePath}",
                ["Seed:Enabled"] = "true",
                ["Seed:DemoPassword"] = ApiConstants.Password
            });
        });

        builder.ConfigureServices(services =>
        {
            var registrations = services.Where(descriptor =>
                descriptor.ServiceType == typeof(MeridianDbContext)
                || (descriptor.ServiceType.IsGenericType
                    && descriptor.ServiceType.GenericTypeArguments.Contains(typeof(MeridianDbContext)))).ToList();

            foreach (var registration in registrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<MeridianDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                File.Delete(_databasePath + suffix);
            }
            catch (IOException)
            {
            }
        }
    }
}

public static class ApiConstants
{
    public const string Password = "Meridian#demo1";
    public const string Maya = "maya.okonkwo@meridian.demo";
    public const string Luis = "luis.ortega@meridian.demo";
    public const string Priya = "priya.nair@meridian.demo";
    public const string Sam = "sam.okada@meridian.demo";
}

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<JsonElement> ReadAsync(this HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    public static async Task<HttpClient> SignInAsync(this MeridianApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ApiConstants.Password }, Options);
        var body = await response.ReadAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Login failed ({(int)response.StatusCode}): {body}");
        }

        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
