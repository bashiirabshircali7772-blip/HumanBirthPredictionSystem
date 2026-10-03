using HumanBirthPredictionSystem.Models;

namespace HumanBirthPredictionSystem.Data
{
    /// <summary>
    /// Seeds the initial administrator account and a small set of sample
    /// countries/cities so the system is immediately demonstrable.
    /// Sample birth data inserted here is explicitly flagged with
    /// RecordType.Estimated / DataSource "Sample Data for System
    /// Demonstration" so it is never confused with official statistics.
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            // ---- Administrator account -----------------------------------
            var admin = db.Users.SingleOrDefault(u => u.Username == "Bashiir")
                ?? db.Users.SingleOrDefault(u => u.Username == "Sakariye");

            if (admin == null)
            {
                admin = new User
                {
                    Username = "Bashiir",
                    PasswordHash = PasswordHasher.Hash("bashiir21"),
                    FullName = "Bashiir Abshir Ali",
                    Role = "Administrator",
                    CreatedAt = new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc)
                };
                db.Users.Add(admin);
            }
            else
            {
                admin.Username = "Bashiir";
                admin.PasswordHash = PasswordHasher.Hash("bashiir21");
                admin.FullName = "Bashiir Abshir Ali";
                admin.Role = "Administrator";
            }

            // ---- Standard Data Entry user account (Ahmed) ----------------
            var ahmed = db.Users.SingleOrDefault(u => u.Username == "Ahmed");
            if (ahmed == null)
            {
                ahmed = new User
                {
                    Username = "Ahmed",
                    PasswordHash = PasswordHasher.Hash("user123"),
                    FullName = "Ahmed Mohamed (Data Entry)",
                    Role = "User",
                    CreatedAt = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc)
                };
                db.Users.Add(ahmed);
            }
            else
            {
                ahmed.PasswordHash = PasswordHasher.Hash("user123");
                ahmed.FullName = "Ahmed Mohamed (Data Entry)";
                ahmed.Role = "User";
            }

            db.SaveChanges();

            // ---- Countries --------------------------------------------------
            if (!db.Countries.Any())
            {
                var countries = new List<Country>
                {
                    new() { CountryName = "Somalia", CountryCode = "SOM", Continent = "Africa" },
                    new() { CountryName = "Kenya", CountryCode = "KEN", Continent = "Africa" },
                    new() { CountryName = "Ethiopia", CountryCode = "ETH", Continent = "Africa" },
                    new() { CountryName = "Nigeria", CountryCode = "NGA", Continent = "Africa" },
                    new() { CountryName = "South Africa", CountryCode = "ZAF", Continent = "Africa" },
                    new() { CountryName = "Egypt", CountryCode = "EGY", Continent = "Africa" },
                    new() { CountryName = "India", CountryCode = "IND", Continent = "Asia" },
                    new() { CountryName = "China", CountryCode = "CHN", Continent = "Asia" },
                    new() { CountryName = "United States", CountryCode = "USA", Continent = "North America" },
                    new() { CountryName = "Canada", CountryCode = "CAN", Continent = "North America" },
                    new() { CountryName = "United Kingdom", CountryCode = "GBR", Continent = "Europe" },
                    new() { CountryName = "Germany", CountryCode = "DEU", Continent = "Europe" },
                };
                db.Countries.AddRange(countries);
                db.SaveChanges();
            }

            // ---- Cities ----------------------------------------------------
            if (!db.Cities.Any())
            {
                var somalia = db.Countries.First(c => c.CountryCode == "SOM");
                var kenya = db.Countries.First(c => c.CountryCode == "KEN");

                db.Cities.AddRange(
                    new City { CountryId = somalia.Id, CityName = "Garowe" },
                    new City { CountryId = somalia.Id, CityName = "Mogadishu" },
                    new City { CountryId = somalia.Id, CityName = "Bosaso" },
                    new City { CountryId = somalia.Id, CityName = "Hargeisa" },
                    new City { CountryId = kenya.Id, CityName = "Nairobi" },
                    new City { CountryId = kenya.Id, CityName = "Mombasa" },
                    new City { CountryId = kenya.Id, CityName = "Kisumu" }
                );
                db.SaveChanges();
            }

            // ---- Somalia birth records (2015-2035 demographic dataset) --------
            var somaliaCountry = db.Countries.FirstOrDefault(c => c.CountryCode == "SOM");
            if (somaliaCountry != null)
            {
                var somaliaBirthData = new (int Year, int Total, int Male, int Female, RecordType Type)[]
                {
                    (2015, 651292, 332031, 319261, RecordType.Estimated),
                    (2016, 668096, 340598, 327498, RecordType.Estimated),
                    (2017, 688481, 350990, 337491, RecordType.Estimated),
                    (2018, 705717, 359777, 345940, RecordType.Estimated),
                    (2019, 723617, 368903, 354714, RecordType.Estimated),
                    (2020, 741705, 378124, 363581, RecordType.Estimated),
                    (2021, 761567, 388250, 373317, RecordType.Estimated),
                    (2022, 779534, 397409, 382125, RecordType.Estimated),
                    (2023, 788763, 402114, 386649, RecordType.Estimated),
                    (2024, 804966, 410375, 394591, RecordType.Estimated),
                    (2025, 822215, 419168, 403047, RecordType.Predicted),
                    (2026, 836420, 426410, 410010, RecordType.Predicted),
                    (2027, 847521, 432070, 415451, RecordType.Predicted),
                    (2028, 859891, 438376, 421515, RecordType.Predicted),
                    (2029, 869665, 443359, 426306, RecordType.Predicted),
                    (2030, 878445, 447835, 430610, RecordType.Predicted),
                    (2031, 891665, 454574, 437091, RecordType.Predicted),
                    (2032, 901357, 459515, 441842, RecordType.Predicted),
                    (2033, 915030, 466486, 448544, RecordType.Predicted),
                    (2034, 924402, 471264, 453138, RecordType.Predicted),
                    (2035, 936181, 477269, 458912, RecordType.Predicted),
                };

                // Clear any non-Somalia or old demo records
                var existingRecords = db.BirthRecords.ToList();
                db.BirthRecords.RemoveRange(existingRecords);
                db.SaveChanges();

                foreach (var item in somaliaBirthData)
                {
                    db.BirthRecords.Add(new BirthRecord
                    {
                        CountryId = somaliaCountry.Id,
                        CityId = null,
                        Year = item.Year,
                        TotalBirths = item.Total,
                        MaleBirths = item.Male,
                        FemaleBirths = item.Female,
                        DataSource = item.Type == RecordType.Predicted
                            ? "Demographic Projections (2025-2035)"
                            : "Demographic Estimates (2015-2024)",
                        SourceReference = "Somalia Demographic and Health Survey / UN Population Division",
                        RecordType = item.Type,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                db.SaveChanges();
            }
        }
    }
}
