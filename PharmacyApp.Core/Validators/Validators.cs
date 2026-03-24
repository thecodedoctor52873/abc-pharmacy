using FluentValidation;
using PharmacyApp.Core.Models;

namespace PharmacyApp.Core.Validators;

public class MedicineValidator : AbstractValidator<Medicine>
{
    public MedicineValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.Today).WithMessage("Expiry date must be in the future");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero");
    }
}

public class SaleValidator : AbstractValidator<Sale>
{
    public SaleValidator()
    {
        RuleFor(x => x.MedicineId).GreaterThan(0);
        RuleFor(x => x.QuantitySold).GreaterThan(0).WithMessage("Quantity sold must be at least 1");
    }
}