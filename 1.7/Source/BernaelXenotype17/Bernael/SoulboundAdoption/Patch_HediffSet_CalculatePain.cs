using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(HediffSet), "CalculatePain")]
    public static class Patch_HediffSet_CalculatePain
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            Pawn parent = __instance.pawn;
            HediffComp_BabyBond_Feeder comp = BabyBondUtility.GetFeederComp(parent);
            if (comp == null)
            {
                return;
            }

            float bondedPain = 0f;
            List<Pawn> children = comp.BondedBabies;
            for (int i = 0; i < children.Count; i++)
            {
                Pawn child = children[i];
                if (child != null && !child.Dead && child.ageTracker.AgeBiologicalYearsFloat < SoulboundAdoptionUtility.BondReleaseAge)
                {
                    bondedPain += child.health.hediffSet.PainTotal;
                }
            }
            __result = Mathf.Clamp01(__result + bondedPain);
        }
    }
}
