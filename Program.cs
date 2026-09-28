using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var railwayConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var useSqlite = builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(railwayConnectionString);

if (useSqlite)
{
    Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "data"));
    configuredConnectionString = "Data Source=data/humanbirth.db";
}

// ---------------------------------------------------------------------
// MVC + Razor
// ---------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------
// Database (SQL Server via Entity Framework Core)
// ---------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(configuredConnectionString);
    }
    else
    {
        options.UseSqlServer(configuredConnectionString);
    }
});

// ---------------------------------------------------------------------
// Cookie Authentication
// ---------------------------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "HumanBirthPredictionSystem.Auth";
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// Application services (Dependency Injection)
// ---------------------------------------------------------------------
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IPythonPredictionService, PythonPredictionService>();

var app = builder.Build();

// ---------------------------------------------------------------------
// Automatic migration + seed on startup (thesis/demo convenience)
// ---------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (useSqlite)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
    DbSeeder.Seed(db);
}

// ---------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
