using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_Verb_MeleeAttack_TryCastShot_HandAnimations
    {
        public static void Postfix(Verb_MeleeAttack __instance)
        {
            Pawn pawn = __instance?.CasterPawn;
            if (pawn is not { Spawned: true, Dead: false, Downed: false }) return;
            if (pawn.stances is { FullBodyBusy: true }) return;

            float cycleSeconds = __instance.verbProps?.AdjustedFullCycleTime(__instance, pawn) ?? 0.5f;
            int durationTicks = Mathf.Clamp(Mathf.RoundToInt(cycleSeconds * 60f * 0.45f), 6, 24);
            HandAnimTracker.GetOrCreate(pawn).Swing.Trigger(Find.TickManager?.TicksGame ?? 0, durationTicks);
        }
    }

    [HarmonyPatch(typeof(Verb_CastAbility), "TryCastShot")]
    public static class Patch_Verb_CastAbility_TryCastShot_HandAnimations
    {
        public static void Postfix(Verb_CastAbility __instance)
        {
            if (__instance?.CasterPawn != null && HandAnimTracker.TryGet(__instance.CasterPawn, out HandAnimState state))
                state.CastRaise.Complete();
        }
    }

    [HarmonyPatch(typeof(Verb_CastAbility), nameof(Verb_CastAbility.TryStartCastOn))]
    public static class Patch_Verb_CastAbility_TryStartCastOn_HandAnimations
    {
        public static void Postfix(Verb_CastAbility __instance, bool __result)
        {
            if (__result && __instance?.CasterPawn != null)
                HandAnimTracker.GetOrCreate(__instance.CasterPawn).CastRaise.Begin(Find.TickManager?.TicksGame ?? 0, __instance.WarmupTime);
        }
    }
}
