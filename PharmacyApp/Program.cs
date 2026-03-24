using FluentValidation;
using PharmacyApp.Core.Models;
using PharmacyApp.Core.Validators;
using Serilog;
using System.Text.Json;

// ── Serilog Setup ────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/pharmacy.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddCors();
builder.Services.AddScoped<IValidator<Medicine>, MedicineValidator>();
builder.Services.AddScoped<IValidator<Sale>, SaleValidator>();

var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Data file path ──────────────────────────────────────────────
var dataFile = Path.Combine(app.Environment.ContentRootPath, "data.json");
var salesFile = Path.Combine(app.Environment.ContentRootPath, "sales.json");
var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

// ── Helpers ─────────────────────────────────────────────────────
List<Medicine> LoadMedicines() =>
    File.Exists(dataFile)
        ? JsonSerializer.Deserialize<List<Medicine>>(File.ReadAllText(dataFile), jsonOpts) ?? []
        : SeedData();

void SaveMedicines(List<Medicine> list) =>
    File.WriteAllText(dataFile, JsonSerializer.Serialize(list, jsonOpts));

List<Sale> LoadSales() =>
    File.Exists(salesFile)
        ? JsonSerializer.Deserialize<List<Sale>>(File.ReadAllText(salesFile), jsonOpts) ?? []
        : [];

void SaveSales(List<Sale> list) =>
    File.WriteAllText(salesFile, JsonSerializer.Serialize(list, jsonOpts));

List<Medicine> SeedData()
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

// ── Medicine Endpoints ───────────────────────────────────────────
app.MapGet("/api/medicines", (string? search) =>
{
    Log.Information("Fetching medicines. Search: {Search}", search ?? "none");
    var list = LoadMedicines();
    if (!string.IsNullOrWhiteSpace(search))
        list = list.Where(m => m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(list);
});

app.MapGet("/api/medicines/{id:int}", (int id) =>
{
    Log.Information("Fetching medicine id: {Id}", id);
    var list = LoadMedicines();
    var med = list.FirstOrDefault(m => m.Id == id);
    return med is null ? Results.NotFound() : Results.Ok(med);
});

app.MapPost("/api/medicines", (Medicine med, IValidator<Medicine> validator) =>
{
    var result = validator.Validate(med);
    if (!result.IsValid)
    {
        Log.Warning("Validation failed for medicine: {Errors}", result.Errors);
        return Results.ValidationProblem(result.ToDictionary());
    }
    var list = LoadMedicines();
    med.Id = list.Count > 0 ? list.Max(m => m.Id) + 1 : 1;
    list.Add(med);
    SaveMedicines(list);
    Log.Information("Medicine added: {Name}", med.FullName);
    return Results.Created($"/api/medicines/{med.Id}", med);
});

app.MapPut("/api/medicines/{id:int}", (int id, Medicine updated, IValidator<Medicine> validator) =>
{
    var result = validator.Validate(updated);
    if (!result.IsValid)
    {
        Log.Warning("Validation failed on update for medicine id {Id}: {Errors}", id, result.Errors);
        return Results.ValidationProblem(result.ToDictionary());
    }
    var list = LoadMedicines();
    var idx = list.FindIndex(m => m.Id == id);
    if (idx == -1) return Results.NotFound();
    updated.Id = id;
    list[idx] = updated;
    SaveMedicines(list);
    Log.Information("Medicine updated: {Id}", id);
    return Results.Ok(updated);
});

app.MapDelete("/api/medicines/{id:int}", (int id) =>
{
    var list = LoadMedicines();
    var med = list.FirstOrDefault(m => m.Id == id);
    if (med is null) return Results.NotFound();
    list.Remove(med);
    SaveMedicines(list);
    Log.Information("Medicine deleted: {Id}", id);
    return Results.NoContent();
});

// ── Sale Endpoints ───────────────────────────────────────────────
app.MapGet("/api/sales", () =>
{
    Log.Information("Fetching all sales");
    return Results.Ok(LoadSales());
});

app.MapPost("/api/sales", (Sale sale, IValidator<Sale> validator) =>
{
    var result = validator.Validate(sale);
    if (!result.IsValid)
    {
        Log.Warning("Validation failed for sale: {Errors}", result.Errors);
        return Results.ValidationProblem(result.ToDictionary());
    }
    var medicines = LoadMedicines();
    var med = medicines.FirstOrDefault(m => m.Id == sale.MedicineId);
    if (med is null) return Results.NotFound("Medicine not found");
    if (med.Quantity < sale.QuantitySold)
    {
        Log.Warning("Insufficient stock for medicine {Id}. Requested: {Qty}, Available: {Stock}", sale.MedicineId, sale.QuantitySold, med.Quantity);
        return Results.BadRequest("Insufficient stock");
    }
    med.Quantity -= sale.QuantitySold;
    SaveMedicines(medicines);
    var sales = LoadSales();
    sale.Id = sales.Count > 0 ? sales.Max(s => s.Id) + 1 : 1;
    sale.SaleDate = DateTime.Now;
    sale.MedicineName = med.FullName;
    sale.TotalAmount = med.Price * sale.QuantitySold;
    sales.Add(sale);
    SaveSales(sales);
    Log.Information("Sale recorded: {MedicineName}, Qty: {Qty}, Total: {Total}", sale.MedicineName, sale.QuantitySold, sale.TotalAmount);
    return Results.Created($"/api/sales/{sale.Id}", sale);
});

app.Run();