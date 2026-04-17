using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Application.Auth.Dtos;
using Application.Organizations.Dtos;

namespace Api.IntegrationTests.Billing;

public class BillingEndpointsTests : IntegrationTestBase
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(string token, Guid orgId)> SetupOwnerWithOrgAsync(string email)
    {
        await Client.PostAsJsonAsync("/auth/register", new { email, password = "Password123!" });
        var loginResp = await Client.PostAsJsonAsync("/auth/login", new { email, password = "Password123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var orgResp = await Client.PostAsJsonAsync("/orgs", new { name = "Test Org" });
        var org = await orgResp.Content.ReadFromJsonAsync<OrgResponse>();

        Client.DefaultRequestHeaders.Remove("X-Organization-Id");
        Client.DefaultRequestHeaders.Add("X-Organization-Id", org!.Id.ToString());

        return (auth.AccessToken, org.Id);
    }

    // ── GET /orgs/{id}/subscription ──────────────────────────────────────────

    [Fact]
    public async Task GetSubscription_NewOrg_Returns204()
    {
        var (_, orgId) = await SetupOwnerWithOrgAsync("sub-owner@test.com");

        var resp = await Client.GetAsync($"/orgs/{orgId}/subscription");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSubscription_NonMember_Returns404()
    {
        var (_, orgId) = await SetupOwnerWithOrgAsync("sub-owner2@test.com");

        // Switch to a different user with no membership
        await Client.PostAsJsonAsync("/auth/register", new { email = "outsider2@test.com", password = "Password123!" });
        var loginResp = await Client.PostAsJsonAsync("/auth/login", new { email = "outsider2@test.com", password = "Password123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        Client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var resp = await Client.GetAsync($"/orgs/{orgId}/subscription");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubscription_Unauthenticated_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization = null;

        var resp = await Client.GetAsync($"/orgs/{Guid.NewGuid()}/subscription");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /billing/checkout ────────────────────────────────────────────────

    [Fact]
    public async Task Checkout_OwnerWithMockService_ReturnsUrl()
    {
        // MockBillingService is active in test env (no STRIPE_SECRET_KEY)
        await SetupOwnerWithOrgAsync("checkout-owner@test.com");

        var resp = await Client.PostAsJsonAsync("/billing/checkout", new { priceId = "price_starter" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["url"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Checkout_Unauthenticated_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization = null;

        var resp = await Client.PostAsJsonAsync("/billing/checkout", new { priceId = "price_starter" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_MissingPriceId_Returns400()
    {
        await SetupOwnerWithOrgAsync("checkout-bad@test.com");

        var resp = await Client.PostAsJsonAsync("/billing/checkout", new { priceId = "" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /billing/portal ──────────────────────────────────────────────────

    [Fact]
    public async Task Portal_WithMockService_ReturnsUrl()
    {
        // MockBillingService always returns a fake portal URL (no Stripe key in test env)
        await SetupOwnerWithOrgAsync("portal-owner@test.com");

        var resp = await Client.PostAsJsonAsync("/billing/portal", new { });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["url"].Should().Contain("billing-mock");
    }

    // ── PUT /orgs/{id} ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrg_Owner_Returns200WithNewName()
    {
        var (_, orgId) = await SetupOwnerWithOrgAsync("update-owner@test.com");

        var resp = await Client.PutAsJsonAsync($"/orgs/{orgId}", new { name = "Renamed Corp" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var org = await resp.Content.ReadFromJsonAsync<OrgResponse>();
        org!.Name.Should().Be("Renamed Corp");
    }

    [Fact]
    public async Task UpdateOrg_EmptyName_Returns400()
    {
        var (_, orgId) = await SetupOwnerWithOrgAsync("update-bad@test.com");

        var resp = await Client.PutAsJsonAsync($"/orgs/{orgId}", new { name = "" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
