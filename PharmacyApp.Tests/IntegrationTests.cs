using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PharmacyApp.Tests;

// ── Test Factory ─────────────────────────────────────────────────
public class PharmacyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _tempPath;

    public PharmacyApiFactory()
    {
        // Each factory instance gets its own isolated temp folder
        _tempPath = Path.Combine(Path.GetTempPath(), "PharmacyTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(_tempPath);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, recursive: true);
    }
}

// ── Medicine Integration Tests ────────────────────────────────────
// Each test class creates its OWN factory = completely fresh isolated data
public class MedicineIntegrationTests : IAsyncLifetime
{
    private PharmacyApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new PharmacyApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetMedicines_ReturnsSeededList_MatchesSnapshot()
    {
        var response = await _client.GetAsync("/api/medicines");

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }

    [Fact]
    public async Task GetMedicines_WithSearch_ReturnsFilteredResult_MatchesSnapshot()
    {
        var response = await _client.GetAsync("/api/medicines?search=Paracetamol");

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }

    [Fact]
    public async Task PostMedicine_ValidPayload_Returns201_MatchesSnapshot()
    {
        var newMedicine = new
        {
            fullName = "Aspirin 100mg",
            brand = "Bayer",
            expiryDate = DateTime.Today.AddDays(60).ToString("yyyy-MM-dd"),
            quantity = 25,
            price = 4.50,
            notes = "Blood thinner"
        };

        var response = await _client.PostAsJsonAsync("/api/medicines", newMedicine);

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date")
            .ScrubLinesContaining("Location"); // scrub ID since it can vary
    }

    [Fact]
    public async Task PostMedicine_InvalidPayload_Returns400_MatchesSnapshot()
    {
        var badMedicine = new
        {
            fullName = "",
            brand = "",
            expiryDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"),
            quantity = -1,
            price = 0
        };

        var response = await _client.PostAsJsonAsync("/api/medicines", badMedicine);

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }

    [Fact]
    public async Task GetMedicine_ById_ReturnsCorrectMedicine_MatchesSnapshot()
    {
        var response = await _client.GetAsync("/api/medicines/1");

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }

    [Fact]
    public async Task GetMedicine_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/medicines/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMedicine_ExistingId_Returns204()
    {
        var response = await _client.DeleteAsync("/api/medicines/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

// ── Sale Integration Tests ────────────────────────────────────────
public class SaleIntegrationTests : IAsyncLifetime
{
    private PharmacyApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new PharmacyApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetSales_InitiallyEmpty_MatchesSnapshot()
    {
        var response = await _client.GetAsync("/api/sales");

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }

    [Fact]
    public async Task PostSale_ValidSale_Returns201_MatchesSnapshot()
    {
        var sale = new { medicineId = 1, quantitySold = 2 };
        var response = await _client.PostAsJsonAsync("/api/sales", sale);

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date")
            .ScrubLinesContaining("Location");
    }

    [Fact]
    public async Task PostSale_InsufficientStock_Returns400()
    {
        var sale = new { medicineId = 2, quantitySold = 9999 };
        var response = await _client.PostAsJsonAsync("/api/sales", sale);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSale_InvalidPayload_Returns400_MatchesSnapshot()
    {
        var sale = new { medicineId = 0, quantitySold = 0 };
        var response = await _client.PostAsJsonAsync("/api/sales", sale);

        await Verify(response)
            .ScrubLinesContaining("date")
            .ScrubLinesContaining("Date");
    }
}