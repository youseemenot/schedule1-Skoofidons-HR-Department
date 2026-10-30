using MelonLoader;

namespace SkoofidonsHRDepartment
{
    public static class Config
    {
        public static MelonPreferences_Category FixerCat;
        public static MelonPreferences_Category PackagerCat;
        public static MelonPreferences_Category ChemistCat;
        public static MelonPreferences_Category BotanistCat;
        public static MelonPreferences_Category CleanerCat;

        // Фиксер
        public static MelonPreferences_Entry<float> FixerFeeLess5;
        public static MelonPreferences_Entry<float> FixerFee5Plus;
        public static MelonPreferences_Entry<float> FixerFeeLimit;

        // Handler (Packager)
        public static MelonPreferences_Entry<int> PackagerMaxRoutes;
        public static MelonPreferences_Entry<int> PackagerMaxStations;
        public static MelonPreferences_Entry<float> PackagerSpeed;
        public static MelonPreferences_Entry<float> PackagerWage;
        public static MelonPreferences_Entry<float> PackagerFee;
        public static MelonPreferences_Entry<float> PackagerWalkSpeed;

        // Химик
        public static MelonPreferences_Entry<int> ChemistMaxStations;
        public static MelonPreferences_Entry<float> ChemistWage;
        public static MelonPreferences_Entry<float> ChemistFee;
        public static MelonPreferences_Entry<float> ChemistWalkSpeed;

        // Ботаник
        public static MelonPreferences_Entry<int> BotanistMaxStations;
        public static MelonPreferences_Entry<float> BotanistWage;
        public static MelonPreferences_Entry<float> BotanistFee;
        public static MelonPreferences_Entry<float> BotanistWalkSpeed;

        // Overrides apply to individual actions; the game's constants stay intact.
        public static MelonPreferences_Entry<bool> BotanistActionTimingEnabled;
        public static MelonPreferences_Entry<float> BotanistSoilTime;
        public static MelonPreferences_Entry<float> BotanistWaterTime;
        public static MelonPreferences_Entry<float> BotanistAdditiveTime;
        public static MelonPreferences_Entry<float> BotanistSeedTime;
        public static MelonPreferences_Entry<float> BotanistHarvestSecondsPerItem;

        // Уборщик
        public static MelonPreferences_Entry<int> CleanerMaxBins;
        public static MelonPreferences_Entry<float> CleanerWage;
        public static MelonPreferences_Entry<float> CleanerFee;
        public static MelonPreferences_Entry<float> CleanerWalkSpeed;

        public static void Initialize()
        {
            FixerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Fixer", "Fixer Settings");
            FixerFeeLess5 = FixerCat.CreateEntry("FeeLess5", 100f, "Fee (<5 Employees)", "Higher = More expensive to hire");
            FixerFee5Plus = FixerCat.CreateEntry("Fee5Plus", 250f, "Fee (5+ Employees)", "Higher = More expensive to hire");
            FixerFeeLimit = FixerCat.CreateEntry("FeeLimit", 500f, "Fee Limit", "Max extra fee amount");

            PackagerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Packager", "Handler Settings");
            PackagerMaxRoutes = PackagerCat.CreateEntry("MaxRoutes", 5, "Max Routes", "Higher = More delivery points");
            PackagerMaxStations = PackagerCat.CreateEntry("MaxStations", 3, "Max Stations", "Higher = Work at more tables");
            PackagerSpeed = PackagerCat.CreateEntry("SpeedMult", 1f, "Packaging Speed", "Higher = Packages boxes faster");
            PackagerWage = PackagerCat.CreateEntry("DailyWage", 200f, "Daily Wage", "Higher = Costs more per day");
            PackagerFee = PackagerCat.CreateEntry("SigningFee", 1000f, "Signing Fee", "Base cost to hire");
            PackagerWalkSpeed = PackagerCat.CreateEntry("WalkSpeed", 1.2f, "Walk Speed", "Higher = Walks faster");

            ChemistCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Chemist", "Chemist Settings");
            ChemistMaxStations = ChemistCat.CreateEntry("MaxStations", 4, "Max Stations", "Higher = Work at more tables");
            ChemistWage = ChemistCat.CreateEntry("DailyWage", 300f, "Daily Wage", "Higher = Costs more per day");
            ChemistFee = ChemistCat.CreateEntry("SigningFee", 1500f, "Signing Fee", "Base cost to hire");
            ChemistWalkSpeed = ChemistCat.CreateEntry("WalkSpeed", 1.2f, "Walk Speed", "Higher = Walks faster");

            BotanistCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Botanist", "Botanist Settings");
            BotanistMaxStations = BotanistCat.CreateEntry("MaxStations", 8, "Max Stations", "Higher = Work at more pots");
            BotanistWage = BotanistCat.CreateEntry("DailyWage", 200f, "Daily Wage", "Higher = Costs more per day");
            BotanistFee = BotanistCat.CreateEntry("SigningFee", 1000f, "Signing Fee", "Base cost to hire");
            BotanistWalkSpeed = BotanistCat.CreateEntry("WalkSpeed", 1.2f, "Walk Speed", "Higher = Walks faster");
            BotanistActionTimingEnabled = BotanistCat.CreateEntry("ActionTimingOverridesEnabled", false,
                "Custom Action Times", "Enable custom durations for newly started botanist actions. Singleplayer test feature.");
            BotanistSoilTime = BotanistCat.CreateEntry("SoilTime", 10f, "Soil Time (seconds)",
                "Base seconds before the employee work-speed adjustment. Applies to the next action.");
            BotanistWaterTime = BotanistCat.CreateEntry("WaterTime", 10f, "Water Time (seconds)",
                "Base seconds before the employee work-speed adjustment. Applies to the next action.");
            BotanistAdditiveTime = BotanistCat.CreateEntry("AdditiveTime", 10f, "Additive Time (seconds)",
                "Base seconds before the employee work-speed adjustment. Applies to the next action.");
            BotanistSeedTime = BotanistCat.CreateEntry("SeedTime", 15f, "Seed Time (seconds)",
                "Base seconds before the employee work-speed adjustment. Applies to the next action.");
            // Old HarvestTime had an ambiguous whole-action default of 15. Do
            // not migrate it to the new seconds-per-unit setting.
            BotanistHarvestSecondsPerItem = BotanistCat.CreateEntry("HarvestSecondsPerItem", 1f,
                "Harvest Seconds Per Item", "Base seconds per harvested unit. Scales with product count and employee work speed.");

            CleanerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Cleaner", "Cleaner Settings");
            CleanerMaxBins = CleanerCat.CreateEntry("MaxBins", 3, "Max Bins", "Higher = Clean more bins");
            CleanerWage = CleanerCat.CreateEntry("DailyWage", 100f, "Daily Wage", "Higher = Costs more per day");
            CleanerFee = CleanerCat.CreateEntry("SigningFee", 500f, "Signing Fee", "Base cost to hire");
            CleanerWalkSpeed = CleanerCat.CreateEntry("WalkSpeed", 1.2f, "Walk Speed", "Higher = Walks faster");

            MelonPreferences.Save();
        }
    }
}
