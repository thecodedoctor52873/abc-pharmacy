using PharmacyApp.Core.Models;

namespace PharmacyApp.Core.Interfaces;

public interface IPharmacyRepository
{
    List<Medicine> GetMedicines();
    Medicine? GetMedicine(int id);
    void SaveMedicines(List<Medicine> medicines);

    List<Sale> GetSales();
    void SaveSales(List<Sale> sales);
}