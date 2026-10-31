using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

if (args.Length != 2) { Console.WriteLine("Usage: ProbeChecks <probe.dll> <game directory>"); return 2; }
var paths = new[] { Path.Combine(args[1], "MelonLoader", "net6"), Path.Combine(args[1], "MelonLoader", "Il2CppAssemblies") };
AssemblyLoadContext.Default.Resolving += (context, name) => {
    string file = paths.Select(p => Path.Combine(p, name.Name + ".dll")).FirstOrDefault(File.Exists);
    return file == null ? null : context.LoadFromAssemblyPath(Path.GetFullPath(file));
};
try
{
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
    if (assembly.GetReferencedAssemblies().Any(a => a.Name!.Contains("Phone", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Probe requires the Phone App");
    int count = 0;
    foreach (var type in assembly.GetTypes())
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null) continue;
            RuntimeHelpers.PrepareMethod(method.MethodHandle); count++;
        }
    Console.WriteLine($"PASS actual probe: {count} managed methods resolve/JIT; no Phone App reference. Native code was not executed.");
    return 0;
}
catch (Exception error) { Console.WriteLine("FAIL actual probe: " + error); return 1; }
