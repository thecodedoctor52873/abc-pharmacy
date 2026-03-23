using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
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
    var list = LoadMedicines();
    if (!string.IsNullOrWhiteSpace(search))
        list = list.Where(m => m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(list);
});

app.MapGet("/api/medicines/{id:int}", (int id) =>
{
    var list = LoadMedicines();
    var med = list.FirstOrDefault(m => m.Id == id);
    return med is null ? Results.NotFound() : Results.Ok(med);
});

app.MapPost("/api/medicines", (Medicine med) =>
{
    var list = LoadMedicines();
    med.Id = list.Count > 0 ? list.Max(m => m.Id) + 1 : 1;
    list.Add(med);
    SaveMedicines(list);
    return Results.Created($"/api/medicines/{med.Id}", med);
});

app.MapPut("/api/medicines/{id:int}", (int id, Medicine updated) =>
{
    var list = LoadMedicines();
    var idx = list.FindIndex(m => m.Id == id);
    if (idx == -1) return Results.NotFound();
    updated.Id = id;
    list[idx] = updated;
    SaveMedicines(list);
    return Results.Ok(updated);
});

app.MapDelete("/api/medicines/{id:int}", (int id) =>
{
    var list = LoadMedicines();
    var med = list.FirstOrDefault(m => m.Id == id);
    if (med is null) return Results.NotFound();
    list.Remove(med);
    SaveMedicines(list);
    return Results.NoContent();
});

// ── Sale Endpoints ───────────────────────────────────────────────
app.MapGet("/api/sales", () => Results.Ok(LoadSales()));

app.MapPost("/api/sales", (Sale sale) =>
{
    // Deduct stock
    var medicines = LoadMedicines();
    var med = medicines.FirstOrDefault(m => m.Id == sale.MedicineId);
    if (med is null) return Results.NotFound("Medicine not found");
    if (med.Quantity < sale.QuantitySold) return Results.BadRequest("Insufficient stock");

    med.Quantity -= sale.QuantitySold;
    SaveMedicines(medicines);

    var sales = LoadSales();
    sale.Id = sales.Count > 0 ? sales.Max(s => s.Id) + 1 : 1;
    sale.SaleDate = DateTime.Now;
    sale.MedicineName = med.FullName;
    sale.TotalAmount = med.Price * sale.QuantitySold;
    sales.Add(sale);
    SaveSales(sales);

    return Results.Created($"/api/sales/{sale.Id}", sale);
});

app.Run();

// ── Models ───────────────────────────────────────────────────────
record Medicine
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Brand { get; set; } = "";
}

record Sale
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = "";
    public int QuantitySold { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; }
}