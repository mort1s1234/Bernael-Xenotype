using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bernael_Xenotype
{
    public static class SoulboundAdoptionUtility
    {
        public const float MaximumAdoptionAge = 13f;
        public const float BondReleaseAge = 14f;
        private static readonly Dictionary<Building_Bed, bool> BedEligibilityCache = [];
        private static int bedEligibilityCacheTick = -1;

        public static bool IsEligibleChild(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike || pawn.ageTracker == null)
            {
                return false;
            }
            if (pawn.ageTracker.AgeBiologicalYearsFloat >= MaximumAdoptionAge || pawn.genes == null)
            {
                return false;
            }
            if (pawn.genes.GetGene(BernaelDefOf.BX_SoulboundAdoption) != null)
            {
                return false;
            }
            return BabyBondUtility.GetBabyComp(pawn) == null;
        }

        public static bool IsEligibleAdopter(Pawn pawn, Building_Bed bed = null)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || !pawn.IsFreeColonist)
            {
                return false;
            }
            if (pawn.genes?.GetGene(BernaelDefOf.BX_SoulboundAdoption) is not Gene_SoulboundAdoption gene || !gene.CanAdoptNow)
            {
                return false;
            }
            return bed == null || pawn.CanReach(bed, PathEndMode.Touch, Danger.Deadly);
        }

        public static bool IsUsableBedFor(Pawn child, Pawn adopter, Building_Bed bed)
        {
            if (!IsEligibleChild(child) || bed == null || !bed.Spawned || !bed.def.building.bed_humanlike)
            {
                return false;
            }
            Pawn bedUser = adopter ?? child;
            return RestUtility.IsValidBedFor(bed, child, bedUser, checkSocialProperness: true, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations: false, child.GuestStatus);
        }

        public static bool BedHasEligiblePair(Building_Bed bed)
        {
            if (bed == null || !bed.Spawned || bed.Map == null)
            {
                return false;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick != bedEligibilityCacheTick)
            {
                BedEligibilityCache.Clear();
                bedEligibilityCacheTick = currentTick;
            }
            if (BedEligibilityCache.TryGetValue(bed, out bool eligible))
            {
                return eligible;
            }

            eligible = CalculateBedHasEligiblePair(bed);
            BedEligibilityCache.Add(bed, eligible);
            return eligible;
        }

        public static bool HasEligibleAdopterForChild(Pawn child, Building_Bed bed)
        {
            if (!IsEligibleChild(child) || bed == null || !bed.Spawned || bed.Map == null)
            {
                return false;
            }

            List<Pawn> adopters = bed.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < adopters.Count; i++)
            {
                Pawn adopter = adopters[i];
                if (IsEligibleAdopter(adopter, bed) && IsUsableBedFor(child, adopter, bed))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CalculateBedHasEligiblePair(Building_Bed bed)
        {
            List<Pawn> children = bed.Map.mapPawns.FreeColonistsAndPrisoners;
            for (int i = 0; i < children.Count; i++)
            {
                if (HasEligibleAdopterForChild(children[i], bed))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryCompleteAdoption(Pawn adopter, Pawn child)
        {
            if (!IsEligibleAdopter(adopter) || !IsEligibleChild(child))
            {
                return false;
            }
            if (!SoulUtility.TryConvertChild(adopter, child))
            {
                return false;
            }

            Gene_SoulboundAdoption gene = adopter.genes.GetGene(BernaelDefOf.BX_SoulboundAdoption) as Gene_SoulboundAdoption;
            gene?.Notify_AdoptionCompleted();
            return true;
        }
    }
}
