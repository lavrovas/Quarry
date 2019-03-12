using System.Reflection;
using Harmony;
using Verse;

// ReSharper disable once CheckNamespace
namespace Quarry {

    [StaticConstructorOnStartup]
    // ReSharper disable once ArrangeTypeModifiers
    // ReSharper disable once UnusedMember.Global
    static class HarmonyPatches {

        static HarmonyPatches() {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.mod.quarry");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("Notify_FogBlockerRemoved")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ArrangeTypeModifiers
    static class FogGrid_Notify_FogBlockerRemoved_Patch {

        // ReSharper disable once ArrangeTypeMemberModifiers
        // ReSharper disable once UnusedMember.Local
        static void Postfix() {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }

    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("Notify_PawnEnteringDoor")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ArrangeTypeModifiers
    // ReSharper disable once UnusedMember.Global
    static class FogGrid_Notify_PawnEnteringDoor_Patch {

        // ReSharper disable once ArrangeTypeMemberModifiers
        // ReSharper disable once UnusedMember.Local
        static void Postfix() {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }

    }

}