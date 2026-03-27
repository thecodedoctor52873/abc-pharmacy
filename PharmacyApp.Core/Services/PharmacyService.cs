using PharmacyApp.Core.Interfaces;
using PharmacyApp.Core.Models;
using Serilog;

namespace PharmacyApp.Core.Services;

public class PharmacyService : IPharmacyService
{
    private readonly IPharmacyRepository _repo;

    public PharmacyService(IPharmacyRepository repo)
    {
        _repo = repo;
    }

    public List<Medicine> GetMedicines(string? search)
    {
        Log.Information("Fetching medicines. Search: {Search}", search ?? "none");
        var list = _repo.GetMedicines();
        if (!string.IsNullOrWhiteSpace(search))
            list = list.Where(m => m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return list;
    }

    public Medicine? GetMedicine(int id)
    {
        Log.Information("Fetching medicine id: {Id}", id);
        return _repo.GetMedicine(id);
    }

    public Medicine AddMedicine(Medicine medicine)
    {
        var list = _repo.GetMedicines();
        medicine.Id = list.Count > 0 ? list.Max(m => m.Id) + 1 : 1;
        list.Add(medicine);
        _repo.SaveMedicines(list);
        Log.Information("Medicine added: {Name}", medicine.FullName);
        return medicine;
    }

    public Medicine? UpdateMedicine(int id, Medicine updated)
    {
        var list = _repo.GetMedicines();
        var idx = list.FindIndex(m => m.Id == id);
        if (idx == -1) return null;
        updated.Id = id;
        list[idx] = updated;
        _repo.SaveMedicines(list);
        Log.Information("Medicine updated: {Id}", id);
        return updated;
    }

    public bool DeleteMedicine(int id)
    {
        var list = _repo.GetMedicines();
        var med = list.FirstOrDefault(m => m.Id == id);
        if (med is null) return false;
        list.Remove(med);
        _repo.SaveMedicines(list);
        Log.Information("Medicine deleted: {Id}", id);
        return true;
    }

    public List<Sale> GetSales()
    {
        Log.Information("Fetching all sales");
        return _repo.GetSales();
    }

    public (Sale? sale, string? error) RecordSale(Sale sale)
    {
        var medicines = _repo.GetMedicines();
        var med = medicines.FirstOrDefault(m => m.Id == sale.MedicineId);
        if (med is null) return (null, "Medicine not found");
        if (med.Quantity < sale.QuantitySold)
        {
            Log.Warning("Insufficient stock for medicine {Id}. Requested: {Qty}, Available: {Stock}",
                sale.MedicineId, sale.QuantitySold, med.Quantity);
            return (null, "Insufficient stock");
        }

        med.Quantity -= sale.QuantitySold;
        _repo.SaveMedicines(medicines);

        var sales = _repo.GetSales();
        sale.Id = sales.Count > 0 ? sales.Max(s => s.Id) + 1 : 1;
        sale.SaleDate = DateTime.Now;
        sale.MedicineName = med.FullName;
        sale.TotalAmount = med.Price * sale.QuantitySold;
        sales.Add(sale);
        _repo.SaveSales(sales);

        Log.Information("Sale recorded: {MedicineName}, Qty: {Qty}, Total: {Total}",
            sale.MedicineName, sale.QuantitySold, sale.TotalAmount);
        return (sale, null);
    }
}