namespace SkoofidonsHRDepartment
{
    public static class PreferenceCategoryOrder
    {
        private const string Prefix = "Skoofidon's HR Department_";

        // Sort keys are transient. Preference identifiers and display names never change.
        public static string GetSortKey(string identifier, string originalKey) => identifier switch
        {
            Prefix + "Botanist" => Prefix + "00_Botanist",
            Prefix + "Chemist" => Prefix + "01_Chemist",
            Prefix + "Cleaner" => Prefix + "02_Cleaner",
            Prefix + "Packager" => Prefix + "03_Handler",
            Prefix + "Fixer" => Prefix + "04_Fixer",
            Prefix + "Machines" => Prefix + "05_Machines",
            _ => originalKey
        };
    }
}
