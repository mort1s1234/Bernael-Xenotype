using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using JetBrains.Annotations;
using System;

namespace Bernael_Xenotype
{
    [StaticConstructorOnStartup]
    public static class BernaelGraceHarmony
    {
        static BernaelGraceHarmony()
        {
            Harmony harmony = new Harmony("BernaelXenotype.Harmony.SentinelPatches");
            harmony.PatchAll();
        }

        [HarmonyPatch]
        public static class Patch_Grace
        {
            public static MethodBase TargetMethod()
            {
                if (!ModsConfig.IsActive("EBSG.Framework")) return null;
                var type = AccessTools.TypeByName("EBSGFramework.ResourceGene");
                return type != null ? AccessTools.Method(type, "Tick") : null;
            }

            [UsedImplicitly]
            private static bool Prepare()
            {
                return ModsConfig.IsActive("Sov.Nephilim") && ModsConfig.IsActive("EBSG.Framework");
            }

            static readonly Type type = AccessTools.TypeByName("EBSGFramework.ResourceGene");
            static readonly FieldInfo geneDef = type != null ? AccessTools.Field(type, "def") : null;
            static readonly FieldInfo creature = type != null ? AccessTools.Field(type, "pawn") : null;

            public static void Postfix(ref Gene_Resource __instance)
            {
                if (type == null || geneDef == null || creature == null || __instance.Value > 0) return;
                Pawn pawn = (Pawn)(creature.GetValue(__instance));
                if (!pawn.IsHashIntervalTick(180) || !pawn.InMentalState || (GeneDef)(geneDef.GetValue(__instance)) != BernaelDefOf.GS_Grace_New || !typeof(Gene_Resource).IsAssignableFrom(__instance.def.geneClass)) return;

                pawn.TurnIntoXenotype(BernaelDefOf.BX_Bernael);
            }
        }

        [HarmonyPatch(typeof(IngestionOutcomeDoer), nameof(IngestionOutcomeDoer.DoIngestionOutcome))]
        public static class Patch_GraceIngestion
        {
            [UsedImplicitly]
            private static bool Prepare()
            {
                return ModsConfig.IsActive("Sov.Nephilim");
            }

            public static void Postfix(Pawn pawn, Thing ingested)
            {
                if (pawn.Map == null || ingested.def.GetGraceOutcomeDoer() == null) return;
                foreach (Pawn otherPawn in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    if (otherPawn.genes == null || otherPawn.relations == null) continue;
                    bool hasHateGene = otherPawn.genes.GetGene(BernaelDefOf.BX_Grace_Galling) != null;
                    if (!hasHateGene) continue;

                    ThoughtHandler memory = otherPawn.needs.mood.thoughts;
                    if (memory == null) continue;

                    TaleRecorder.RecordTale(BernaelDefOf.BX_ConsumedGrace, pawn, otherPawn);
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
        public static class Patch_WeirdSpeech
        {
            public static void Postfix(Pawn_InteractionsTracker __instance, ref bool __result, Pawn recipient)
            {
                if (!__result) return;
                Pawn instigator = __instance.pawn;
                if (instigator.genes?.GetGene(BernaelDefOf.BX_DarkSpeech) == null) return;
                if (recipient.needs.mood.thoughts == null || recipient.genes == null || recipient.genes.GetGene(BernaelDefOf.BX_DarkSpeech) != null) return;
                recipient.needs.mood.thoughts.memories.TryGainMemory(BernaelDefOf.BX_HeardDarkSpeech);
            }
        }

        [HarmonyPatch(typeof(Recipe_Surgery), nameof(Recipe_Surgery.AvailableOnNow))]
        public static class Patch_NoEyeSurgery
        {
            public static void Postfix(Thing thing, BodyPartRecord part, ref bool __result)
            {
                Pawn pawn = thing as Pawn;
                if (pawn == null || pawn.genes?.GetGene(BernaelDefOf.BX_DepravedHead) == null || part?.def != BodyPartDefOf.Eye) return;
                __result = false;
            }
        }

        [HarmonyPatch(typeof(ShotReport), nameof(ShotReport.HitReportFor))]
        public static class Patch_VisibilityThroughGas
        {
            static Gamecomponent_PsychicSight gameComp => Current.Game.GetGamePsychicSightComp();
            static FieldInfo fieldInfo => AccessTools.Field(typeof(ShotReport), "factorFromCoveringGas");

            public static void Postfix(Thing caster, ref ShotReport __result)
            {
                if (gameComp == null || caster as Pawn == null) return;

                if (gameComp.psychicSeers.NullOrEmpty())
                    gameComp.psychicSeers = new HashSet<Pawn>();

                if (!gameComp.psychicSeers.Contains((Pawn)caster)) return;

                fieldInfo.SetValueDirect(__makeref(__result), 1f);
                //Log.Error($"{(float)(fieldInfo.GetValue(__result))}");
            }
        }

        [HarmonyPatch(typeof(PawnCapacityWorker_Sight), nameof(PawnCapacityWorker_Sight.CalculateCapacityLevel))]
        public static class Patch_SightStat
        {
            static Gamecomponent_PsychicSight gameComp => Current.Game.GetGamePsychicSightComp();
            public static void Postfix(HediffSet diffSet, ref float __result)
            {
                if (__result != 1.25f && !gameComp.psychicSeers.NullOrEmpty() && gameComp.psychicSeers.Contains(diffSet.pawn))
                {
                    __result = 1.25f;
                    //Log.Error($"{__result}");
                }
            }
        }
    }
}
