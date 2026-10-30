using System;

namespace SkoofidonsHRDepartment.Diagnostics
{
    public static class ProbeProtocol
    {
        public const string ExpectedGameSha512 = "872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A";

        public static bool MatchesGame(string hash) => string.Equals(hash, ExpectedGameSha512, StringComparison.OrdinalIgnoreCase);
        public static bool ShouldLog(int priorSamples) => priorSamples >= 0 && priorSamples < 8;
        public static bool IsFirstWait(int before, int after, bool running) => before == 0 && after == 1 && running;

        public static string Classify(string nativeType) => nativeType switch {
            "ScheduleOne.NPCs.Behaviour.AddSoilToGrowContainerBehaviour" => "soil",
            "ScheduleOne.NPCs.Behaviour.WaterPotBehaviour" => "water",
            "ScheduleOne.NPCs.Behaviour.ApplyAdditiveToGrowContainerBehaviour" => "additive",
            "ScheduleOne.NPCs.Behaviour.SowSeedInPotBehaviour" => "seed",
            "ScheduleOne.NPCs.Behaviour.HarvestPotBehaviour" => "harvest",
            _ => "other"
        };
    }
}
