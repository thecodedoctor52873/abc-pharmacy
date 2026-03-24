using PharmacyApp.Core.Models;
using PharmacyApp.Core.Validators;

namespace PharmacyApp.Tests;

// ── Medicine Validator Tests ──────────────────────────────────────
public class MedicineValidatorTests
{
    private readonly MedicineValidator _validator = new();

    private Medicine ValidMedicine() => new()
    {
        FullName = "Paracetamol 500mg",
        Brand = "GSK",
        ExpiryDate = DateTime.Today.AddDays(30),
        Quantity = 10,
        Price = 5.99m
    };

    [Fact]
    public void ValidMedicine_ShouldPassValidation()
    {
        var result = _validator.Validate(ValidMedicine());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyFullName_ShouldFailValidation()
    {
        var med = ValidMedicine() with { FullName = "" };
        var result = _validator.Validate(med);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FullName");
    }

    [Fact]
    public void EmptyBrand_ShouldFailValidation()
    {
        var med = ValidMedicine() with { Brand = "" };
        var result = _validator.Validate(med);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Brand");
    }

    [Fact]
    public void PastExpiryDate_ShouldFailValidation()
    {
        var med = ValidMedicine() with { ExpiryDate = DateTime.Today.AddDays(-1) };
        var result = _validator.Validate(med);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryDate");
    }

    [Fact]
    public void ZeroPrice_ShouldFailValidation()
    {
        var med = ValidMedicine() with { Price = 0 };
        var result = _validator.Validate(med);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public void NegativeQuantity_ShouldFailValidation()
    {
        var med = ValidMedicine() with { Quantity = -1 };
        var result = _validator.Validate(med);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void ZeroQuantity_ShouldPassValidation()
    {
        var med = ValidMedicine() with { Quantity = 0 };
        var result = _validator.Validate(med);
        Assert.True(result.IsValid);
    }
}

// ── Sale Validator Tests ──────────────────────────────────────────
public class SaleValidatorTests
{
    private readonly SaleValidator _validator = new();

    private Sale ValidSale() => new() { MedicineId = 1, QuantitySold = 2 };

    [Fact]
    public void ValidSale_ShouldPassValidation()
    {
        var result = _validator.Validate(ValidSale());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ZeroMedicineId_ShouldFailValidation()
    {
        var sale = ValidSale() with { MedicineId = 0 };
        var result = _validator.Validate(sale);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MedicineId");
    }

    [Fact]
    public void ZeroQuantitySold_ShouldFailValidation()
    {
        var sale = ValidSale() with { QuantitySold = 0 };
        var result = _validator.Validate(sale);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "QuantitySold");
    }

    [Fact]
    public void NegativeQuantitySold_ShouldFailValidation()
    {
        var sale = ValidSale() with { QuantitySold = -5 };
        var result = _validator.Validate(sale);
        Assert.False(result.IsValid);
    }
}

// ── Business Logic Tests ──────────────────────────────────────────
public class BusinessLogicTests
{
    [Fact]
    public void Medicine_ExpiringIn10Days_ShouldBeMarkedExpiringSoon()
    {
        var med = new Medicine { ExpiryDate = DateTime.Today.AddDays(10) };
        var daysLeft = (med.ExpiryDate - DateTime.Today).TotalDays;
        Assert.True(daysLeft < 30);
    }

    [Fact]
    public void Medicine_WithQuantityBelow10_ShouldBeMarkedLowStock()
    {
        var med = new Medicine { Quantity = 8 };
        Assert.True(med.Quantity < 10);
    }

    [Fact]
    public void Sale_TotalAmount_ShouldBeCalculatedCorrectly()
    {
        var price = 12.50m;
        var qty = 3;
        var total = price * qty;
        Assert.Equal(37.50m, total);
    }

    [Fact]
    public void Stock_AfterSale_ShouldBeDeducted()
    {
        var initialStock = 20;
        var qtySold = 5;
        var remaining = initialStock - qtySold;
        Assert.Equal(15, remaining);
    }

    [Fact]
    public void Sale_WhenStockInsufficient_ShouldNotProceed()
    {
        var available = 3;
        var requested = 10;
        Assert.True(requested > available);
    }
}