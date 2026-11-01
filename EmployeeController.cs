using System.Collections.Generic;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.DevUtilities;
using Il2CppInterop.Runtime.InteropTypes;

namespace SkoofidonsHRDepartment
{
    public static class EmployeeController
    {
        private static readonly List<Employee> ActiveEmployees = new List<Employee>();

        public static void AddEmployee(Employee emp)
        {
            if (emp != null && !ActiveEmployees.Contains(emp))
            {
                ActiveEmployees.Add(emp);
            }
        }

        public static void ClearCache()
        {
            EmployeeSpeedController.RestoreAndClear();
            ActiveEmployees.Clear();
        }

        public static void ApplyToSpawnedEmployees()
        {
            foreach (var emp in ActiveEmployees) ApplyStats(emp, false);
        }

        public static void ApplyToPrefabs()
        {
            var manager = NetworkSingleton<EmployeeManager>.Instance;
            if (manager == null) return;

            ApplyStats(manager.GetEmployeePrefab(EEmployeeType.Botanist), true);
            ApplyStats(manager.GetEmployeePrefab(EEmployeeType.Handler), true);
            ApplyStats(manager.GetEmployeePrefab(EEmployeeType.Chemist), true);
            ApplyStats(manager.GetEmployeePrefab(EEmployeeType.Cleaner), true);
        }

        public static void ApplyStats(Employee emp, bool isPrefab = false)
        {
            if (emp == null) return;

            string typeName = emp.GetIl2CppType().Name;
            EmployeeSpeedController.Apply(emp, isPrefab);

            // 1. ОБЩИЕ ХАРАКТЕРИСТИКИ (БЕЗОПАСНО)
            if (typeName == "Packager")
            {
                emp.DailyWage = Config.PackagerWage.Value;
                emp.SigningFee = Config.PackagerFee.Value;
            }
            else if (typeName == "Chemist")
            {
                emp.DailyWage = Config.ChemistWage.Value;
                emp.SigningFee = Config.ChemistFee.Value;
            }
            else if (typeName == "Botanist")
            {
                emp.DailyWage = Config.BotanistWage.Value;
                emp.SigningFee = Config.BotanistFee.Value;
            }
            else if (typeName == "Cleaner")
            {
                emp.DailyWage = Config.CleanerWage.Value;
                emp.SigningFee = Config.CleanerFee.Value;
            }

            if (!isPrefab)
            {
                try { var home = emp.GetHome(); if (home != null) home.UpdateStorageText(); } catch { }
            }

            // 2. УНИКАЛЬНЫЕ ХАРАКТЕРИСТИКИ (ТОЛЬКО НЕ-СТАТИЧЕСКИЕ)
            if (typeName == "Packager")
            {
                var packager = emp.Cast<Packager>();
                ApplyHandlerLimits(packager);
            }
            else if (typeName == "Chemist")
            {
                ApplyChemistLimits(emp.Cast<Chemist>());
            }
            else if (typeName == "Botanist")
            {
                ApplyBotanistLimits(emp.Cast<Botanist>());
            }
            else if (typeName == "Cleaner")
            {
                ApplyCleanerLimits(emp.Cast<Cleaner>());
            }
        }

        public static void ApplyBotanistLimits(Botanist botanist)
        {
            botanist.MaxAssignedPots = Config.BotanistMaxStations.Value;
            // The game copies MaxAssignedPots into Assigns only at construction.
            // Existing employees must also update the selector used by the clipboard.
            var configuration = botanist.configuration;
            if (configuration?.Assigns != null)
                configuration.Assigns.MaxItems = Config.BotanistMaxStations.Value;
        }

        public static void ApplyChemistLimits(Chemist chemist)
        {
            var configuration = chemist.configuration;
            if (configuration?.Stations != null)
                configuration.Stations.MaxItems = Config.ChemistMaxStations.Value;
        }

        public static void ApplyCleanerLimits(Cleaner cleaner)
        {
            var configuration = cleaner.configuration;
            if (configuration?.Bins != null)
                configuration.Bins.MaxItems = Config.CleanerMaxBins.Value;
        }

        public static void ApplyHandlerLimits(Packager packager)
        {
            packager.MaxAssignedStations = Config.PackagerMaxStations.Value;
            var configuration = packager.configuration;
            if (configuration == null) return;
            // The constructor copies the employee limit once. Existing employees
            // also need the live field used by selection and its Assigned label.
            if (configuration.Stations != null)
                configuration.Stations.MaxItems = Config.PackagerMaxStations.Value;
            if (configuration.Routes != null)
                configuration.Routes.MaxRoutes = Config.PackagerMaxRoutes.Value;
        }

    }
}
