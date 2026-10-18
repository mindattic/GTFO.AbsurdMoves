using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace GTFO.AbsurdMoves;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInProcess("GTFO.exe")]
[BepInDependency("dev.gtfomodding.gtfo-api")]
public class Plugin : BasePlugin
{
    public const string GUID = "MindAttic.GTFO.AbsurdMoves";
    public const string NAME = "GTFO.AbsurdMoves";
    public const string VERSION = "1.0.0";

    internal static ManualLogSource Logger = null!;

    public override void Load()
    {
        Logger = Log;
        Dodge.Bind(Config);
        BulletTime.Bind(Config);
        AirMoves.Bind(Config);
        Slide.Bind(Config);
        GroundPound.Bind(Config);

        ClassInjector.RegisterTypeInIl2Cpp<LoopSounds>();
        ClassInjector.RegisterTypeInIl2Cpp<BulletTime>();
        ClassInjector.RegisterTypeInIl2Cpp<PoundPushes>();
        BulletTime.Init();
        GroundPound.Init();

        new Harmony(GUID).PatchAll();
        AddComponent<LoopSounds>();
        AddComponent<BulletTime>();
        AddComponent<PoundPushes>();

        Logger.LogInfo($"{NAME} v{VERSION} loaded.");
    }
}
