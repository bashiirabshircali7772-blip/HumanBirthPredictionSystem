# Human Birth Prediction and Gender Distribution System

**Thesis:** Human Birth Trends and Gender Distribution: A Predictive Analysis Using Python (2025–2035)

A full-stack analytical information system that manages historical birth
records, analyzes birth trends and gender distribution across countries,
and predicts future birth patterns using a Python / Scikit-learn machine
learning module.

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9 MVC, C#, Entity Framework Core 9, Dependency Injection |
| Database | Microsoft SQL Server (`localhost\SQLEXPRESS01`, DB `HumanBirthPredictionDB`) |
| Prediction | Python 3, Pandas, NumPy, Scikit-learn (Linear Regression) |
| Frontend | Razor Views, HTML5/CSS3, Chart.js |
| Auth | ASP.NET Core Cookie Authentication, PBKDF2 password hashing |

---

## 2. Project Structure

```
HumanBirthPredictionSystem/
  Controllers/        AccountController, DashboardController, CountriesController,
                       CitiesController, BirthRecordsController,
                       GenderDistributionController, BirthTrendsController,
                       PredictionController, ReportsController
  Models/              User, Country, City, BirthRecord, Prediction, Enums (RecordType)
  Data/                ApplicationDbContext, DbSeeder, PasswordHasher
  Services/            IDashboardService/DashboardService,
                       IAnalyticsService/AnalyticsService,
                       IPythonPredictionService/PythonPredictionService
  ViewModels/          LoginViewModel, DashboardViewModel, GenderDistributionViewModel,
                       BirthTrendsViewModel, PredictionViewModel, ReportsViewModel
  Views/               Account, Dashboard, Countries, Cities, BirthRecords,
                       GenderDistribution, BirthTrends, Prediction, Reports, Shared
  wwwroot/             css/site.css, js/site.js
  python/              prediction.py, data_processing.py, requirements.txt
  Database/            schema.sql (manual fallback schema)
  Program.cs, appsettings.json
```

Every major component maps directly onto the thesis brief's architecture:

```
USER → ASP.NET CORE MVC → SQL SERVER
                ↓
      PYTHON PREDICTION MODULE (python/prediction.py)
                ↓
      PREDICTION RESULTS → SQL SERVER (Predictions table)
                ↓
      DASHBOARD → INTERACTIVE CHARTS & REPORTS
```

---

## 3. Prerequisites

1. **.NET 9 SDK** — https://dotnet.microsoft.com/download
2. **SQL Server Express** (instance named `SQLEXPRESS01`) or adjust the
   connection string in `appsettings.json` to match your instance.
3. **Python 3.9+** with `pip`, available on your system `PATH` as `python`
   (or `python3` — update `PythonSettings:PythonExecutablePath` in
   `appsettings.json` if your executable is named differently).

---

## 4. Setup Steps

### 4.1 Install Python dependencies

```bash
python -m pip install --default-timeout=3600 --retries 10 -r python/requirements.txt
```

You can sanity-check the prediction module standalone before running the
web app:

```bash
echo '{"start_year":2025,"end_year":2030,"history":[{"year":2020,"total_births":42000,"male_births":21500,"female_births":20500},{"year":2021,"total_births":43500,"male_births":22200,"female_births":21300},{"year":2022,"total_births":45000,"male_births":23000,"female_births":22000}]}' | python prediction.py
```

### 4.2 Configure the database connection

`appsettings.json` already targets:

```
Server=localhost\SQLEXPRESS01;Database=HumanBirthPredictionDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Change this if your SQL Server instance name is different.

### 4.3 Create the database with EF Core Migrations (recommended)

From the project root (where `HumanBirthPredictionSystem.csproj` lives):

```bash
dotnet tool install --global dotnet-ef   # first time only
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

`Program.cs` also calls `db.Database.Migrate()` automatically at startup,
so once a migration exists, simply running the app will apply it and seed
the initial data (see 4.5).

**Alternative:** if you prefer not to use the EF Core CLI tools, run
`Database/schema.sql` directly in SQL Server Management Studio against a
new `HumanBirthPredictionDB` database. In that case, remove or comment
out the `db.Database.Migrate()` call in `Program.cs`, since there won't be
any EF migrations to apply — the seeder (`DbSeeder.Seed`) will still run
and insert the admin account, countries, cities and demonstration data.

### 4.4 Run the application

```bash
dotnet run
```

Navigate to the URL shown in the console (e.g. `https://localhost:7080`).

### 4.5 Log in

The system seeds one administrator account on first run:

| Username | Password |
|---|---|
| `Bashiir` | `bashiir21` |

The password is stored using PBKDF2-SHA256 (100,000 iterations, random
salt) — never in plain text. See `Data/PasswordHasher.cs`.

---

## 5. How the Python Integration Works

1. The administrator selects a Country, optional City, and a year range
   on the **Prediction** page and clicks **Run Prediction**.
2. `PredictionController.Run()` calls `PythonPredictionService`, which:
   - Retrieves the relevant historical `BirthRecord` rows from SQL Server
     via Entity Framework Core.
   - Serializes them to JSON and starts `python/prediction.py` as a child
     process, piping the JSON in via `stdin`.
3. `prediction.py`:
   - Cleans the data with Pandas (`data_processing.py`).
   - Trains a `sklearn.linear_model.LinearRegression` model on
     Year → TotalBirths.
   - Predicts totals for every year in the requested range, then splits
     each total into Male/Female estimates using the historical average
     gender ratio.
   - Prints a single JSON object to `stdout`.
4. `PythonPredictionService` deserializes the result, and
   `PredictionController` saves the rows into the `Predictions` table
   (never into `BirthRecords`, keeping predicted data structurally
   separate from recorded data) and displays them alongside historical
   data, clearly labeled.

**Extending the model:** because `prediction.py` only exchanges JSON over
stdin/stdout, you can add Polynomial Regression, Random Forest, or ARIMA
by writing a new `fit_and_predict_xxx()` function and switching
`MODEL_NAME` / the function call in `main()` — no changes are required on
the ASP.NET Core side.

---

## 6. Data Integrity Rules Implemented

- `MaleBirths + FemaleBirths` must equal `TotalBirths` (enforced in
  `BirthRecordsController.ValidateRecordAsync` and as a `CHECK` constraint
  in `Database/schema.sql`).
- Birth values cannot be negative.
- A City, if selected, must belong to the selected Country.
- Duplicate records for the same Country/City/Year/RecordType are
  rejected.
- `RecordType` distinguishes **Official**, **Historical**, **Estimated**,
  and **Predicted** data; predictions are stored in a separate
  `Predictions` table so they can never be displayed as officially
  recorded statistics. The Prediction page shows a disclaimer stating
  this explicitly.
- Demonstration data seeded by `DbSeeder.cs` is explicitly labeled
  `"Sample Data for System Demonstration"` in the `DataSource` field.

---

## 7. Default Seed Data

`Data/DbSeeder.cs` seeds:
- The administrator account.
- 12 countries (Somalia, Kenya, Ethiopia, Nigeria, South Africa, Egypt,
  India, China, United States, Canada, United Kingdom, Germany).
- Cities under Somalia (Garowe, Mogadishu, Bosaso, Hargeisa) and Kenya
  (Nairobi, Mombasa, Kisumu).
- Ten years (2015–2024) of randomized, clearly-labeled sample birth
  records per country, so charts and the dashboard are populated
  immediately for a defense demonstration. Replace these with real
  historical statistics (with proper `DataSource` / `SourceReference`
  citations) before presenting real analysis.

---

## 8. Suggested Defense Talking Points

- Show the **architecture diagram** above and trace a live request:
  Prediction page → Controller → SQL Server → Python subprocess →
  Scikit-learn → back to Controller → SQL Server → Dashboard chart.
- Demonstrate the **validation rules** (try entering Male + Female ≠
  Total on the Birth Records form).
- Show the **Historical vs Predicted** distinction on the Prediction page
  and the disclaimer text.
- Open `python/prediction.py` and explain the Linear Regression training
  step, and how the architecture supports swapping in Random Forest or
  ARIMA later without touching the web layer.
- Use the **Reports** page (Print / Export PDF button) to generate a
  clean printable report for the appendix of the thesis document.
