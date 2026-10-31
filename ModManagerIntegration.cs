using MelonLoader;

namespace SkoofidonsHRDepartment
{
    public static class ModManagerIntegration
    {
        public static void Initialize() => OptionalPhoneIntegration.Initialize(MelonBase.RegisteredMelons);
        public static void Deinitialize() => OptionalPhoneIntegration.Shutdown();
    }
}
