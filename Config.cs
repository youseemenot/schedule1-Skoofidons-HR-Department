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
        public static MelonPreferences_Category MachinesCat;
        public static MelonPreferences_Entry<float> MachineChemistryRate;
        public static MelonPreferences_Entry<float> MachineOvenRate;
        public static MelonPreferences_Entry<float> MachineMixingRate;
        public static MelonPreferences_Entry<float> MachineCauldronRate;
        public static MelonPreferences_Entry<float> MachineDryingRate;
        public static MelonPreferences_Entry<float> MachinePlantRate;
        public static MelonPreferences_Entry<float> MachineShroomRate;

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
        public static MelonPreferences_Entry<float> ChemistWorkSpeed;

        // Ботаник
        public static MelonPreferences_Entry<int> BotanistMaxStations;
        public static MelonPreferences_Entry<float> BotanistWage;
        public static MelonPreferences_Entry<float> BotanistFee;
        public static MelonPreferences_Entry<float> BotanistWalkSpeed;

        public static MelonPreferences_Entry<float> BotanistWorkSpeed;

        // Уборщик
        public static MelonPreferences_Entry<int> CleanerMaxBins;
        public static MelonPreferences_Entry<float> CleanerWage;
        public static MelonPreferences_Entry<float> CleanerFee;
        public static MelonPreferences_Entry<float> CleanerWalkSpeed;
        public static MelonPreferences_Entry<float> CleanerWorkSpeed;

        private static MelonPreferences_Entry<float> Movement(MelonPreferences_Category category) =>
            category.CreateEntry("WalkSpeedMultiplier", 1f, "Move speed x (higher = faster)",
                "1 = game normal; 7 = seven times faster; 0.5 = half speed. Relative to the employee's original movement speed.");

        private static MelonPreferences_Entry<float> WorkRate(MelonPreferences_Category category) =>
            category.CreateEntry("WorkSpeedMultiplier", 1f, "Work speed x (higher = less time)",
                "1 = normal time; 7 = normal time divided by seven; 0.5 = twice as long. Preserves native work-speed effects and quantities.");

        public static void Initialize()
        {
            FixerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Fixer", "Fixer Settings");
            FixerFeeLess5 = FixerCat.CreateEntry("FeeLess5", 100f, "Fee (<5 Employees)", "Higher = More expensive to hire");
            FixerFee5Plus = FixerCat.CreateEntry("Fee5Plus", 250f, "Fee (5+ Employees)", "Higher = More expensive to hire");
            FixerFeeLimit = FixerCat.CreateEntry("FeeLimit", 500f, "Fee Limit", "Max extra fee amount");

            PackagerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Packager", "Handler Settings");
            PackagerMaxRoutes = PackagerCat.CreateEntry("MaxRoutes", 5, "Max routes (higher = more routes)", "Higher = More delivery points");
            PackagerMaxStations = PackagerCat.CreateEntry("MaxStations", 3, "Max stations (higher = more stations)", "Higher = Work at more tables");
            PackagerSpeed = WorkRate(PackagerCat);
            PackagerWage = PackagerCat.CreateEntry("DailyWage", 200f, "Daily Wage", "Higher = Costs more per day");
            PackagerFee = PackagerCat.CreateEntry("SigningFee", 1000f, "Signing Fee", "Base cost to hire");
            PackagerWalkSpeed = Movement(PackagerCat);

            ChemistCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Chemist", "Chemist Settings");
            ChemistMaxStations = ChemistCat.CreateEntry("MaxStations", 4, "Max stations (higher = more stations)", "Higher = Work at more tables");
            ChemistWage = ChemistCat.CreateEntry("DailyWage", 300f, "Daily Wage", "Higher = Costs more per day");
            ChemistFee = ChemistCat.CreateEntry("SigningFee", 1500f, "Signing Fee", "Base cost to hire");
            ChemistWalkSpeed = Movement(ChemistCat);
            ChemistWorkSpeed = WorkRate(ChemistCat);

            BotanistCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Botanist", "Botanist Settings");
            BotanistMaxStations = BotanistCat.CreateEntry("MaxStations", 8, "Max pots (higher = more pots)", "Higher = Work at more pots");
            BotanistWage = BotanistCat.CreateEntry("DailyWage", 200f, "Daily Wage", "Higher = Costs more per day");
            BotanistFee = BotanistCat.CreateEntry("SigningFee", 1000f, "Signing Fee", "Base cost to hire");
            BotanistWalkSpeed = Movement(BotanistCat);
            // One common work rate replaces the experimental five settings and switch.
            BotanistWorkSpeed = WorkRate(BotanistCat);

            CleanerCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Cleaner", "Cleaner Settings");
            CleanerMaxBins = CleanerCat.CreateEntry("MaxBins", 3, "Max bins (higher = more bins)", "Higher = Clean more bins");
            CleanerWage = CleanerCat.CreateEntry("DailyWage", 100f, "Daily Wage", "Higher = Costs more per day");
            CleanerFee = CleanerCat.CreateEntry("SigningFee", 500f, "Signing Fee", "Base cost to hire");
            CleanerWalkSpeed = Movement(CleanerCat);
            CleanerWorkSpeed = WorkRate(CleanerCat);

            MachinesCat = MelonPreferences.CreateCategory("Skoofidon's HR Department_Machines", "Machine Timers");
            MachineChemistryRate = MachineRate("ChemistryRate", "Chemistry");
            MachineOvenRate = MachineRate("OvenRate", "Oven");
            MachineMixingRate = MachineRate("MixingRate", "Mixing");
            MachineCauldronRate = MachineRate("CauldronRate", "Cauldron");
            MachineDryingRate = MachineRate("DryingRate", "Drying");
            MachinePlantRate = MachineRate("PlantRate", "Plant growth");
            MachineShroomRate = MachineRate("ShroomRate", "Mushroom growth");

            MelonPreferences.Save();
        }

        private static MelonPreferences_Entry<float> MachineRate(string key, string title) =>
            MachinesCat.CreateEntry(key, 1f, title + " x (higher = less time)",
                "New operations only: 1 = normal; 7 = seven times faster; 0.5 = half speed. The captured factor survives saving/loading. Existing operations retain their factor; employee actions are separate.");
    }
}
