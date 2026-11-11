using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Makers.Api;
using Makers.Application;
using Makers.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Makers.Api.Tests;

public sealed class LoanApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly ApiFactory factory = factory;

    [Fact]
    public async Task UserCanCreateAndReadOnlyOwnLoans()
    {
        await LoginAsync("usuario@test.com");
        var createdResponse = await client.PostAsJsonAsync("/api/loans", new LoanInput(1_250_000m, 18));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<LoanView>();
        Assert.NotNull(created);

        var page = await client.GetFromJsonAsync<Page<LoanView>>("/api/loans?page=1&size=100");
        Assert.NotNull(page);
        Assert.Contains(page.Items, loan => loan.Id == created.Id);
        Assert.All(page.Items, loan => Assert.Equal(created.UserId, loan.UserId));
    }

    [Fact]
    public async Task UserCannotDecideAndAdminCanDecideOnce()
    {
        await LoginAsync("usuario@test.com");
        var createdResponse = await client.PostAsJsonAsync("/api/loans", new LoanInput(900_000m, 12));
        var created = await createdResponse.Content.ReadFromJsonAsync<LoanView>();
        Assert.NotNull(created);
        var decision = new DecisionInput("Approved", "Capacidad validada");

        var forbidden = await client.PatchAsJsonAsync($"/api/loans/{created.Id}/decision", decision);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        await LoginAsync("admin@test.com");
        var snapshot = await client.GetAsync("/api/analytics/loans");
        Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
        Assert.DoesNotContain("@test.com", await snapshot.Content.ReadAsStringAsync());
        var approved = await client.PatchAsJsonAsync($"/api/loans/{created.Id}/decision", decision);
        Assert.True(approved.StatusCode == HttpStatusCode.OK,
            $"Decisión falló con {(int)approved.StatusCode}: {await approved.Content.ReadAsStringAsync()}");
        var conflict = await client.PatchAsJsonAsync($"/api/loans/{created.Id}/decision",
            new DecisionInput("Rejected", "Segundo intento"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    private async Task LoginAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var seeded = await users.FindByEmailAsync(email, CancellationToken.None);
        Assert.NotNull(seeded);
        Assert.True(passwords.Verify(seeded, "Demo123456!"), $"El hash seed no coincide para {email}.");
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Demo123456!"));
        Assert.True(response.IsSuccessStatusCode,
            $"Login falló con {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            json.RootElement.GetProperty("accessToken").GetString());
    }
}
