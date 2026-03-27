using System.Runtime.CompilerServices;
using VerifyTests;

namespace PharmacyApp.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        UseProjectRelativeDirectory("Snapshots");
    }
}