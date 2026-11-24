using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class RitualObligationTargetWorker_SoulboundAdoption : RitualObligationTargetFilter
    {
        public RitualObligationTargetWorker_SoulboundAdoption()
        {
        }

        public RitualObligationTargetWorker_SoulboundAdoption(RitualObligationTargetFilterDef def) : base(def)
        {
        }

        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] is Building_Bed bed && CanUseBed(bed))
                {
                    yield return bed;
                }
            }
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            return target.Thing is Building_Bed bed && CanUseBed(bed);
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            yield return "BX_SoulboundAdoptionTargetInfo".Translate();
        }

        private static bool CanUseBed(Building_Bed bed)
        {
            return bed.Faction == Faction.OfPlayer && SoulboundAdoptionUtility.BedHasEligiblePair(bed);
        }
    }
}
