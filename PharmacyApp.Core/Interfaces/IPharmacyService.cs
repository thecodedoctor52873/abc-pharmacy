using PharmacyApp.Core.Models;

namespace PharmacyApp.Core.Interfaces;

public interface IPharmacyService
{
    List<Medicine> GetMedicines(string? search);
    Medicine? GetMedicine(int id);
    Medicine AddMedicine(Medicine medicine);
    Medicine? UpdateMedicine(int id, Medicine medicine);
    bool DeleteMedicine(int id);

    List<Sale> GetSales();
    (Sale? sale, string? error) RecordSale(Sale sale);
}