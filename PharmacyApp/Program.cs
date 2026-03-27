using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using PharmacyApp.Core.Interfaces;
using PharmacyApp.Core.Models;
using PharmacyApp.Core.Repositories;
using PharmacyApp.Core.Services;
using PharmacyApp.Core.Validators;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;
using System.Threading.RateLimiting;

// ── Serilog Setup ────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/pharmacy.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddCors();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }
        ));
    options.RejectionStatusCode = 429;
});
builder.Services.AddOpenApi();

// ── Register Repository, Service, Validators ─────────────────────
builder.Services.AddScoped<IPharmacyRepository, JsonPharmacyRepository>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IValidator<Medicine>, MedicineValidator>();
builder.Services.AddScoped<IValidator<Sale>, SaleValidator>();

var app = builder.Build();

// ── Global Exception Handler ─────────────────────────────────────
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred.",
            detail = app.Environment.IsDevelopment() ? ex.Message : null
        });
    }
});

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

// ── Medicine Endpoints ───────────────────────────────────────────
app.MapGet("/api/medicines", (string? search, IPharmacyService svc) =>
    Results.Ok(svc.GetMedicines(search)));

app.MapGet("/api/medicines/{id:int}", (int id, IPharmacyService svc) =>
{
var med = svc.GetMedicine(id);
return med is null ? Results.NotFound() : Results.Ok(med);
});

app.MapPost("/api/medicines", (Medicine med, IValidator<Medicine> validator, IPharmacyService svc) =>
{
var result = validator.Validate(med);
if (!result.IsValid)
{
Log.Warning("Validation failed for medicine: {Errors}", result.Errors);
return Results.ValidationProblem(result.ToDictionary());
}
var created = svc.AddMedicine(med);
return Results.Created($"/api/medicines/{created.Id}", created);
});

app.MapPut("/api/medicines/{id:int}", (int id, Medicine updated, IValidator<Medicine> validator, IPharmacyService svc) =>
{
var result = validator.Validate(updated);
if (!result.IsValid)
{
Log.Warning("Validation failed on update for medicine id {Id}: {Errors}", id, result.Errors);
return Results.ValidationProblem(result.ToDictionary());
}
var med = svc.UpdateMedicine(id, updated);
return med is null ? Results.NotFound() : Results.Ok(med);
});

app.MapDelete("/api/medicines/{id:int}", (int id, IPharmacyService svc) =>
    svc.DeleteMedicine(id) ? Results.NoContent() : Results.NotFound());

// ── Sale Endpoints ───────────────────────────────────────────────
app.MapGet("/api/sales", (IPharmacyService svc) =>
    Results.Ok(svc.GetSales()));

app.MapPost("/api/sales", (Sale sale, IValidator<Sale> validator, IPharmacyService svc) =>
{
var result = validator.Validate(sale);
if (!result.IsValid)
{
Log.Warning("Validation failed for sale: {Errors}", result.Errors);
return Results.ValidationProblem(result.ToDictionary());
}
var (created, error) = svc.RecordSale(sale);
if (error == "Medicine not found") return Results.NotFound(error);
if (error == "Insufficient stock") return Results.BadRequest(error);
return Results.Created($"/api/sales/{created!.Id}", created);
});

app.Run();

public partial class Program { }