using HarmonyLib;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(Bill_Medical), "PawnAllowedToStartAnew")]
    public static class BillMedical_PawnAllowedToStartAnew
    {
        static void Postfix(Bill_Medical __instance, Pawn pawn, ref bool __result)
        {
            if (!__result) return;
            if (__instance.recipe == BernaelDefOf.BX_ExtractSoul)
            {
                __result = pawn.genes?.HasActiveGene(BernaelDefOf.BX_SoulStarved) ?? false;
            }
        }
    }
    [HarmonyPatch(typeof(Bill_Medical), "ShouldDoNow")]
    public static class BillMedical_ShouldDoNow
    {
        static void Postfix(Bill_Medical __instance, ref bool __result)
        {
            if (!__result) return;
            if (__instance.recipe == BernaelDefOf.BX_ExtractSoul)
            {
                foreach (Pawn colonist in __instance.GiverPawn.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist.genes?.HasActiveGene(BernaelDefOf.BX_SoulStarved) == true && colonist.workSettings?.WorkIsActive(WorkTypeDefOf.Doctor) == true && !colonist.Downed)
                    {
                        __result = true;
                    }
                }
            }
        }
    }

}
