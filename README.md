# ABC Pharmacy — Medicine Manager

A Single Page Application built with **.NET 10 Web API** and vanilla **JavaScript** to manage medicines and sales records.

## Features

- View all medicines in a grid
- 🔴 Red highlight — expiry date less than 30 days
- 🟡 Yellow highlight — stock quantity less than 10
- Search medicines by name
- Add / Edit / Delete medicines
- Record sales (auto-deducts stock)
- View full sales history
- Data stored in JSON files on the server

## Tech Stack

- **Backend:** ASP.NET Core Minimal API (.NET 10)
- **Frontend:** Vanilla JavaScript, HTML, CSS (Single Page)
- **Storage:** JSON files (`data.json`, `sales.json`)

## How to Run

**Step 1:** Clone the repo
```bash
git clone https://github.com/YOUR_USERNAME/abc-pharmacy.git
cd abc-pharmacy
```

**Step 2:** Run the project
```bash
dotnet run --project PharmacyApp
```

**Step 3:** Open your browser at
```
http://localhost:5000
```

## Project Structure

```
PharmacyApp/
├── Program.cs            # All API endpoints + models
├── PharmacyApp.csproj    # .NET 10 project file
├── data.json             # Medicine data (auto-created)
├── sales.json            # Sales records (auto-created)
├── Properties/
│   └── launchSettings.json
└── wwwroot/
    └── index.html        # Full frontend
```
