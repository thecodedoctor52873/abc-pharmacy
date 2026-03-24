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
- **Validation:** FluentValidation
- **Logging:** Serilog (console + rolling file)
- **Tests:** xUnit (16 tests)
- **CI/CD:** GitHub Actions → Docker Hub

## How to Run

**Step 1:** Clone the repo
```bash
git clone https://github.com/thecodedoctor52873/abc-pharmacy.git
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

## Run Tests

```bash
dotnet test PharmacyApp.Tests
```

## Project Structure

```
abc-pharmacy/
├── PharmacyApp/                  # Web API
│   ├── Program.cs                # All API endpoints
│   ├── PharmacyApp.csproj
│   ├── Properties/
│   │   └── launchSettings.json
│   └── wwwroot/
│       └── index.html            # Full frontend (SPA)
├── PharmacyApp.Core/             # Shared models & validators
│   ├── Models/
│   │   ├── Medicine.cs
│   │   └── Sale.cs
│   └── Validators/
│       └── Validators.cs
├── PharmacyApp.Tests/            # xUnit tests (16 tests)
│   └── PharmacyTests.cs
├── Dockerfile
└── .github/
    └── workflows/
        └── docker-publish.yml    # CI/CD pipeline
```
