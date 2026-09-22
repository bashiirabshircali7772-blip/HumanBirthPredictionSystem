using System.Diagnostics;
using System.Text.Json;
using HumanBirthPredictionSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Services
{
    /// <summary>
    /// Bridges ASP.NET Core and the Python data-analysis / machine-learning
    /// module. Historical BirthRecords are pulled from SQL Server here,
    /// serialized to JSON, and piped into python/prediction.py via stdin.
    /// The script performs cleaning (Pandas), trains a Scikit-learn Linear
    /// Regression model, and returns predictions as JSON on stdout.
    ///
    /// The Python module is intentionally decoupled from ASP.NET Core: it
    /// only knows about a generic "year -> totals" series, so additional
    /// models (Polynomial Regression, Random Forest, ARIMA, etc.) can be
    /// added later by extending prediction.py without touching this class.
    /// </summary>
    public class PythonPredictionService : IPythonPredictionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<PythonPredictionService> _logger;
        private readonly IWebHostEnvironment _env;

        public PythonPredictionService(
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<PythonPredictionService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _env = env;
        }

        public async Task<PredictionResult> RunPredictionAsync(PredictionRequest request)
        {
            // 1-3: Retrieve + filter historical records from SQL Server
            var query = _db.BirthRecords.AsNoTracking()
                .Where(r => r.CountryId == request.CountryId);

            if (request.CityId.HasValue)
                query = query.Where(r => r.CityId == request.CityId);

            var records = await query.OrderBy(r => r.Year).ToListAsync();

            if (records.Count < 2)
            {
                return new PredictionResult
                {
                    Success = false,
                    ErrorMessage = "At least two years of historical data are required to run a prediction for this selection."
                };
            }

            var payload = new
            {
                start_year = request.StartYear,
                end_year = request.EndYear,
                history = records.Select(r => new
                {
                    year = r.Year,
                    total_births = r.TotalBirths,
                    male_births = r.MaleBirths,
                    female_births = r.FemaleBirths
                })
            };

            var inputJson = JsonSerializer.Serialize(payload);

            try
            {
                var pythonExe = _config["PythonSettings:PythonExecutablePath"] ?? "python";
                var scriptsFolder = _config["PythonSettings:ScriptsFolder"] ?? "python";
                var scriptName = _config["PythonSettings:PredictionScript"] ?? "prediction.py";
                var timeoutSeconds = int.TryParse(_config["PythonSettings:TimeoutSeconds"], out var t) ? t : 60;

                var scriptPath = Path.Combine(_env.ContentRootPath, scriptsFolder, scriptName);

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to start the Python prediction process.");

                await process.StandardInput.WriteAsync(inputJson);
                process.StandardInput.Close();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                var finished = process.WaitForExit(timeoutSeconds * 1000);
                if (!finished)
                {
                    process.Kill(true);
                    return new PredictionResult { Success = false, ErrorMessage = "The prediction process timed out." };
                }

                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                {
                    _logger.LogError("Python prediction script failed. Exit code: {Code}. Stderr: {Err}", process.ExitCode, stderr);
                    return new PredictionResult { Success = false, ErrorMessage = "The prediction module reported an error. See server logs for details." };
                }

                var parsed = JsonSerializer.Deserialize<PythonScriptOutput>(stdout, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed is null || !parsed.success)
                {
                    return new PredictionResult { Success = false, ErrorMessage = parsed?.error ?? "Unknown prediction error." };
                }

                return new PredictionResult
                {
                    Success = true,
                    Model = parsed.model ?? "Linear Regression",
                    Predictions = parsed.predictions.Select(p => new PredictedYear
                    {
                        Year = p.year,
                        TotalBirths = p.total_births,
                        MaleBirths = p.male_births,
                        FemaleBirths = p.female_births
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking the Python prediction module.");
                return new PredictionResult
                {
                    Success = false,
                    ErrorMessage = "Could not run the Python prediction module. Ensure Python and the required libraries (pandas, numpy, scikit-learn) are installed and accessible on PATH."
                };
            }
        }

        // DTOs matching the JSON contract emitted by prediction.py
        private class PythonScriptOutput
        {
            public bool success { get; set; }
            public string? error { get; set; }
            public string? model { get; set; }
            public List<PythonPredictedYear> predictions { get; set; } = new();
        }

        private class PythonPredictedYear
        {
            public int year { get; set; }
            public int total_births { get; set; }
            public int male_births { get; set; }
            public int female_births { get; set; }
        }
    }
}
