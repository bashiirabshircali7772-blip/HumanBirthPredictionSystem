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
                    Role = "Administrator"
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

            // ---- Sample historical birth records (clearly labeled) --------
            if (!db.BirthRecords.Any())
            {
                var rnd = new Random(42);
                var records = new List<BirthRecord>();

                foreach (var country in db.Countries.ToList())
                {
                    // Base births vary per country so charts look realistic
                    double baseTotal = 40000 + rnd.Next(0, 400000);

                    for (int year = 2015; year <= 2024; year++)
                    {
                        // Slight randomized year-over-year growth/decline
                        baseTotal *= 1 + (rnd.NextDouble() * 0.04 - 0.01);
                        int total = (int)baseTotal;
                        int male = (int)(total * (0.512 + (rnd.NextDouble() * 0.006 - 0.003)));
                        int female = total - male;

                        records.Add(new BirthRecord
                        {
                            CountryId = country.Id,
                            CityId = null,
                            Year = year,
                            TotalBirths = total,
                            MaleBirths = male,
                            FemaleBirths = female,
                            DataSource = "Sample Data for System Demonstration",
                            SourceReference = "Generated for thesis defense demonstration purposes",
                            RecordType = RecordType.Estimated
                        });
                    }
                }

                db.BirthRecords.AddRange(records);
                db.SaveChanges();
            }
        }
    }
}
