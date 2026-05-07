using System.Collections.Generic;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.DevUtilities;
using Il2CppInterop.Runtime.InteropTypes;
using System;

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

        public static void ClearCache() => ActiveEmployees.Clear();

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

            // 1. ОБЩИЕ ХАРАКТЕРИСТИКИ (БЕЗОПАСНО)
            if (typeName == "Packager")
            {
                emp.DailyWage = Config.PackagerWage.Value;
                emp.SigningFee = Config.PackagerFee.Value;
                if (emp.Movement != null) emp.Movement.WalkSpeed = Config.PackagerWalkSpeed.Value;
            }
            else if (typeName == "Chemist")
            {
                emp.DailyWage = Config.ChemistWage.Value;
                emp.SigningFee = Config.ChemistFee.Value;
                if (emp.Movement != null) emp.Movement.WalkSpeed = Config.ChemistWalkSpeed.Value;
            }
            else if (typeName == "Botanist")
            {
                emp.DailyWage = Config.BotanistWage.Value;
                emp.SigningFee = Config.BotanistFee.Value;
                if (emp.Movement != null) emp.Movement.WalkSpeed = Config.BotanistWalkSpeed.Value;
            }
            else if (typeName == "Cleaner")
            {
                emp.DailyWage = Config.CleanerWage.Value;
                emp.SigningFee = Config.CleanerFee.Value;
                if (emp.Movement != null) emp.Movement.WalkSpeed = Config.CleanerWalkSpeed.Value;
            }

            if (!isPrefab)
            {
                try { var home = emp.GetHome(); if (home != null) home.UpdateStorageText(); } catch { }
            }

            // 2. УНИКАЛЬНЫЕ ХАРАКТЕРИСТИКИ (ТОЛЬКО НЕ-СТАТИЧЕСКИЕ)
            if (typeName == "Packager")
            {
                var packager = emp.Cast<Packager>();
                packager.MaxAssignedStations = Config.PackagerMaxStations.Value;
                packager.PackagingSpeedMultiplier = Config.PackagerSpeed.Value;
                if (packager.configuration != null && packager.configuration.Routes != null)
                    packager.configuration.Routes.MaxRoutes = Config.PackagerMaxRoutes.Value;
            }
            else if (typeName == "Chemist")
            {
                var chemist = emp.Cast<Chemist>();
                if (chemist.configuration != null && chemist.configuration.Stations != null)
                    chemist.configuration.Stations.MaxItems = Config.ChemistMaxStations.Value;
            }
            else if (typeName == "Botanist")
            {
                var botanist = emp.Cast<Botanist>();
                // Это поле не статическое, менять можно!
                botanist.MaxAssignedPots = Config.BotanistMaxStations.Value;

                // ТАЙМИНГИ БОТАНИКА УДАЛЕНЫ ИЗ-ЗА ЗАЩИТЫ ПАМЯТИ ИГРЫ
                // (SoilPourTime, WaterPourTime и др. вызывали краш)
            }
            else if (typeName == "Cleaner")
            {
                var cleaner = emp.Cast<Cleaner>();
                if (cleaner.configuration != null && cleaner.configuration.Bins != null)
                    cleaner.configuration.Bins.MaxItems = Config.CleanerMaxBins.Value;
            }
        }
    }
}