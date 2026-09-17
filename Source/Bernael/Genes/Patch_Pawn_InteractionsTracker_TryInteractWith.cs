using HarmonyLib;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static class Patch_Pawn_InteractionsTracker_TryInteractWith
    {
        public static void Postfix(ref bool __result, Pawn recipient, Pawn ___pawn)
        {
            if (!__result) return;
            Pawn instigator = ___pawn;
            if (instigator.genes?.GetGene(BernaelDefOf.BX_DarkSpeech) == null) return;
            if (recipient.needs.mood.thoughts == null || recipient.genes == null || recipient.genes.GetGene(BernaelDefOf.BX_DarkSpeech) != null) return;
            recipient.needs.mood.thoughts.memories.TryGainMemory(BernaelDefOf.BX_HeardDarkSpeech);
        }
    }
}
