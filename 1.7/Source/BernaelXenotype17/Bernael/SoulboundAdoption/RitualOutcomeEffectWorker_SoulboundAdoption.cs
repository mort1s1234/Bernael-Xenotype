using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class RitualOutcomeEffectWorker_SoulboundAdoption : RitualOutcomeEffectWorker
    {
        public override bool ShowQuality => false;

        public RitualOutcomeEffectWorker_SoulboundAdoption()
        {
        }

        public RitualOutcomeEffectWorker_SoulboundAdoption(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn adopter = jobRitual.assignments.FirstAssignedPawn("adopter");
            Pawn child = jobRitual.assignments.FirstAssignedPawn("child");
            if (!SoulboundAdoptionUtility.TryCompleteAdoption(adopter, child))
            {
                Messages.Message("BX_SoulboundAdoptionFailed".Translate(), jobRitual.selectedTarget, MessageTypeDefOf.NegativeEvent);
                return;
            }

            Messages.Message("BX_SoulboundAdoptionCompleted".Translate(adopter.Named("ADOPTER"), child.Named("CHILD")), child, MessageTypeDefOf.PositiveEvent);
        }
    }
}
