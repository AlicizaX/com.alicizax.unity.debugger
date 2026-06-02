using System.Reflection;
using System.Runtime.CompilerServices;
using AlicizaX.Console.Utilities;

namespace AlicizaX.Console
{
    public class AlicizaXConsoleScanRuleset
    {
        private static readonly string[] BannedAssemblyPrefixes =
        {
            "System", "Unity", "Microsoft", "Mono.", "mscorlib", "NSubstitute", "JetBrains", "nunit.",
            "GeNa."
#if AlicizaXConsole_DISABLE_BUILTIN_ALL
            , "AlicizaX.Debugger"
#elif AlicizaXConsole_DISABLE_BUILTIN_EXTRA
            , "AlicizaX.Console.Extra"
#endif
        };

        private static readonly string[] BannedAssemblyNames =
        {
            "mcs", "AssetStoreTools", "Facepunch.Steamworks"
        };

        public bool ShouldScan<T>(T entity) where T : ICustomAttributeProvider
        {
            if (entity.HasAttribute<AlicizaXConsoleIgnoreAttribute>(false))
            {
                return false;
            }

            if (!(entity is MemberInfo) && entity.HasAttribute<CompilerGeneratedAttribute>(true))
            {
                return false;
            }

            return !(entity is Assembly assembly) || ShouldScanAssembly(assembly);
        }

        private static bool ShouldScanAssembly(Assembly assembly)
        {
            string assemblyFullName = assembly.FullName;
            foreach (string prefix in BannedAssemblyPrefixes)
            {
                if (assemblyFullName.StartsWith(prefix))
                {
                    return false;
                }
            }

            string assemblyShortName = assembly.GetName().Name;
            foreach (string name in BannedAssemblyNames)
            {
                if (assemblyShortName == name)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
