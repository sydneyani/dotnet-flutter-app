using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Meridian.Api.Tests;

public class HealthCheckTests(MeridianApiFactory factory) : IClassFixture<MeridianApiFactory>
{
    [Fact]
    public async Task Live_and_ready_report_healthy()
    {
        var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");
        var readyBody = await ready.ReadAsync();

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal("Healthy", readyBody.GetProperty("status").GetString());
        Assert.Contains(readyBody.GetProperty("checks").EnumerateArray(), check => check.GetProperty("name").GetString() == "database");
    }
}

public class AuthFlowTests(MeridianApiFactory factory) : IClassFixture<MeridianApiFactory>
{
    [Fact]
    public async Task Login_rejects_a_bad_password_and_me_requires_a_token()
    {
        var client = factory.CreateClient();
        var rejected = await client.PostAsJsonAsync("/api/auth/login", new { email = ApiConstants.Maya, password = "not-the-password" }, TestJson.Options);
        var anonymous = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_and_reuse_of_the_old_token_is_rejected()
    {
        var client = factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/login", new { email = ApiConstants.Maya, password = ApiConstants.Password }, TestJson.Options)).ReadAsync();
        var firstRefresh = login.GetProperty("refreshToken").GetString();

        var rotated = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = firstRefresh }, TestJson.Options);
        var rotatedBody = await rotated.ReadAsync();
        var reused = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = firstRefresh }, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        Assert.NotEqual(firstRefresh, rotatedBody.GetProperty("refreshToken").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var client = factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/login", new { email = ApiConstants.Luis, password = ApiConstants.Password }, TestJson.Options)).ReadAsync();
        var refresh = login.GetProperty("refreshToken").GetString();

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = refresh }, TestJson.Options);
        var again = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh }, TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);
    }
}

public class WorkOrderFlowTests(MeridianApiFactory factory) : IClassFixture<MeridianApiFactory>
{
    [Fact]
    public async Task Technician_can_open_start_and_complete_a_work_order()
    {
        var client = await factory.SignInAsync(ApiConstants.Maya);
        var assets = await (await client.GetAsync("/api/assets?search=TR-1044")).ReadAsync();
        var assetId = assets.GetProperty("items")[0].GetProperty("id").GetGuid();

        var createdResponse = await client.PostAsJsonAsync("/api/work-orders", new
        {
            title = "Replace cabinet heater on TR-1044",
            description = "Heater circuit was open on the morning round. Confirm the thermostat before closing the cabinet.",
            assetId,
            type = "Corrective",
            priority = "High",
            assignedToUserId = "11111111-1111-4111-8111-111111111111",
            dueAt = DateTimeOffset.UtcNow.AddDays(3)
        }, TestJson.Options);
        var created = await createdResponse.ReadAsync();
        var id = created.GetProperty("id").GetGuid();

        var started = await client.PostAsJsonAsync($"/api/work-orders/{id}/transitions", new { status = "InProgress" }, TestJson.Options);
        var completed = await client.PostAsJsonAsync($"/api/work-orders/{id}/transitions", new { status = "Completed", laborHours = 1.5 }, TestJson.Options);
        var commented = await client.PostAsJsonAsync($"/api/work-orders/{id}/comments", new { body = "Heater drew 4.2 A after the thermostat was replaced." }, TestJson.Options);
        var detail = await commented.ReadAsync();

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("WO-00011", created.GetProperty("number").GetString());
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal("Completed", detail.GetProperty("status").GetString());
        Assert.Equal(1.5m, detail.GetProperty("laborHours").GetDecimal());
        Assert.Contains(detail.GetProperty("comments").EnumerateArray(), comment => comment.GetProperty("body").GetString()!.Contains("4.2 A", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Illegal_transition_is_a_business_rule_problem()
    {
        var client = await factory.SignInAsync(ApiConstants.Priya);
        var openOrders = await (await client.GetAsync("/api/work-orders?status=Open&search=SWG-A12")).ReadAsync();
        var id = openOrders.GetProperty("items")[0].GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"/api/work-orders/{id}/transitions", new { status = "Completed", laborHours = 1 }, TestJson.Options);
        var problem = await response.ReadAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("Cannot move", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Technician_cannot_close_another_technicians_job()
    {
        var client = await factory.SignInAsync(ApiConstants.Maya);
        var luisJobs = await (await client.GetAsync("/api/work-orders?search=PMP-CW-3")).ReadAsync();
        var id = luisJobs.GetProperty("items")[0].GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"/api/work-orders/{id}/transitions", new { status = "InProgress" }, TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Blank_title_returns_a_validation_problem()
    {
        var client = await factory.SignInAsync(ApiConstants.Maya);
        var response = await client.PostAsJsonAsync("/api/work-orders", new
        {
            title = "",
            description = "Missing a title.",
            assetId = "bbbbbbb1-0000-4000-8000-000000000001",
            type = "Preventive",
            priority = "Low"
        }, TestJson.Options);
        var problem = await response.ReadAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("title", out JsonElement titleErrors));
        Assert.True(titleErrors.GetArrayLength() > 0);
    }
}

public class AssetAndDashboardTests(MeridianApiFactory factory) : IClassFixture<MeridianApiFactory>
{
    [Fact]
    public async Task Dashboard_matches_the_seeded_field_picture()
    {
        var client = await factory.SignInAsync(ApiConstants.Priya);
        var summary = await (await client.GetAsync("/api/dashboard/summary")).ReadAsync();

        Assert.Equal(3, summary.GetProperty("openWorkOrders").GetInt32());
        Assert.Equal(2, summary.GetProperty("inProgressWorkOrders").GetInt32());
        Assert.Equal(2, summary.GetProperty("waitingPartsWorkOrders").GetInt32());
        Assert.Equal(2, summary.GetProperty("overdueWorkOrders").GetInt32());
        Assert.Equal(2, summary.GetProperty("completedLast30Days").GetInt32());
        Assert.Equal(3.0m, summary.GetProperty("averageLaborHoursCompleted").GetDecimal());
        Assert.Equal(8, summary.GetProperty("assetsInService").GetInt32());
        Assert.Equal(1, summary.GetProperty("assetsOutOfService").GetInt32());
        Assert.Equal(2, summary.GetProperty("criticalAssets").GetInt32());
        Assert.Equal(1, summary.GetProperty("failedInspectionsLast30Days").GetInt32());
    }

    [Fact]
    public async Task Technician_cannot_edit_the_asset_register_and_an_admin_can()
    {
        var technician = await factory.SignInAsync(ApiConstants.Maya);
        var sites = await (await technician.GetAsync("/api/sites")).ReadAsync();
        var siteId = sites.EnumerateArray().First(site => site.GetProperty("code").GetString() == "SUB-NR-04").GetProperty("id").GetGuid();
        var payload = new
        {
            siteId,
            assetTag = "rel-90b",
            name = "Bus differential relay",
            category = "ProtectiveRelay",
            status = "InService",
            criticality = "High",
            manufacturer = "Kestrel Protection",
            model = "K90-B",
            serialNumber = "KP-90B-14",
            installedOn = "2021-05-19",
            description = "87B protection added during the North Range bus rebuild."
        };

        var forbidden = await technician.PostAsJsonAsync("/api/assets", payload, TestJson.Options);
        var admin = await factory.SignInAsync(ApiConstants.Sam);
        var created = await admin.PostAsJsonAsync("/api/assets", payload, TestJson.Options);
        var duplicate = await admin.PostAsJsonAsync("/api/assets", payload, TestJson.Options);
        var body = await created.ReadAsync();

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("REL-90B", body.GetProperty("assetTag").GetString());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Failed_inspection_requires_findings_and_a_pass_updates_the_asset()
    {
        var client = await factory.SignInAsync(ApiConstants.Luis);
        var assetId = Guid.Parse("bbbbbbb2-0000-4000-8000-000000000002");
        var rejected = await client.PostAsJsonAsync("/api/inspections", new
        {
            assetId,
            inspectedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            result = "Fail",
            notes = "Density alarm was lit.",
            findings = ""
        }, TestJson.Options);

        var recorded = await client.PostAsJsonAsync("/api/inspections", new
        {
            assetId,
            inspectedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            result = "Pass",
            notes = "Gas density alarm cleared after the heater cycle. No leak soap.",
            nextDueOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180)).ToString("yyyy-MM-dd")
        }, TestJson.Options);
        var asset = await (await client.GetAsync($"/api/assets/{assetId}")).ReadAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(asset.GetProperty("lastInspectedAt").GetString()));
    }
}
