using System.Reflection;
using Harmony;
using Verse;

namespace Quarry
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.mod.quarry");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("Notify_FogBlockerRemoved")]
    static class FogGrid_Notify_FogBlockerRemoved_Patch
    {
        static void Postfix()
        {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }
    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("Notify_PawnEnteringDoor")]
    static class FogGrid_Notify_PawnEnteringDoor_Patch
    {
        static void Postfix()
        {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }
    }
}
