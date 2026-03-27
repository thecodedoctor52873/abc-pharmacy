using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using PharmacyApp.Core.Interfaces;
using PharmacyApp.Core.Models;
using Serilog;

namespace PharmacyApp.Core.Repositories;

public class JsonPharmacyRepository : IPharmacyRepository
{
    private readonly string _dataFile;
    private readonly string _salesFile;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOpts;

    private const string MedicineCacheKey = "medicines";
    private const string SalesCacheKey = "sales";

    public JsonPharmacyRepository(IMemoryCache cache, IHostEnvironment env)
    {
        _cache = cache;
        _dataFile = Path.Combine(env.ContentRootPath, "data.json");
        _salesFile = Path.Combine(env.ContentRootPath, "sales.json");
        _jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
    }

    public List<Medicine> GetMedicines()
    {
        if (_cache.TryGetValue(MedicineCacheKey, out List<Medicine>? cached) && cached is not null)
        {
            Log.Debug("Medicines served from cache");
            return cached;
        }

        var list = File.Exists(_dataFile)
            ? JsonSerializer.Deserialize<List<Medicine>>(File.ReadAllText(_dataFile), _jsonOpts) ?? []
            : Seed();

        _cache.Set(MedicineCacheKey, list, TimeSpan.FromMinutes(5));
        return list;
    }

    public Medicine? GetMedicine(int id) =>
        GetMedicines().FirstOrDefault(m => m.Id == id);

    public void SaveMedicines(List<Medicine> medicines)
    {
        File.WriteAllText(_dataFile, JsonSerializer.Serialize(medicines, _jsonOpts));
        _cache.Remove(MedicineCacheKey);
    }

    public List<Sale> GetSales()
    {
        if (_cache.TryGetValue(SalesCacheKey, out List<Sale>? cached) && cached is not null)
        {
            Log.Debug("Sales served from cache");
            return cached;
        }

        var list = File.Exists(_salesFile)
            ? JsonSerializer.Deserialize<List<Sale>>(File.ReadAllText(_salesFile), _jsonOpts) ?? []
            : [];

        _cache.Set(SalesCacheKey, list, TimeSpan.FromMinutes(5));
        return list;
    }

    public void SaveSales(List<Sale> sales)
    {
        File.WriteAllText(_salesFile, JsonSerializer.Serialize(sales, _jsonOpts));
        _cache.Remove(SalesCacheKey);
    }

    private List<Medicine> Seed()
    {
        Log.Information("Seeding initial medicine data");
        var list = new List<Medicine>
        {
            new() { Id=1, FullName="Paracetamol 500mg", Notes="Common painkiller", ExpiryDate=DateTime.Today.AddDays(10), Quantity=50, Price=5.99m, Brand="GSK" },
            new() { Id=2, FullName="Amoxicillin 250mg", Notes="Antibiotic", ExpiryDate=DateTime.Today.AddDays(120), Quantity=8, Price=12.50m, Brand="Cipla" },
            new() { Id=3, FullName="Ibuprofen 400mg", Notes="Anti-inflammatory", ExpiryDate=DateTime.Today.AddDays(200), Quantity=30, Price=8.75m, Brand="Pfizer" },
            new() { Id=4, FullName="Metformin 500mg", Notes="Diabetes medication", ExpiryDate=DateTime.Today.AddDays(25), Quantity=5, Price=3.20m, Brand="Sun Pharma" },
            new() { Id=5, FullName="Atorvastatin 10mg", Notes="Cholesterol", ExpiryDate=DateTime.Today.AddDays(300), Quantity=20, Price=15.00m, Brand="Ranbaxy" },
        };
        SaveMedicines(list);
        return list;
    }
}