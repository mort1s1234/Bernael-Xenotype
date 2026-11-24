using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Bernael_Xenotype
{
    public class RitualBehaviorWorker_SoulboundAdoption : RitualBehaviorWorker
    {
        public override bool ChecksReservations => false;

        public RitualBehaviorWorker_SoulboundAdoption()
        {
        }

        public RitualBehaviorWorker_SoulboundAdoption(RitualBehaviorDef def) : base(def)
        {
        }

        public override bool PawnCanFillRole(Pawn pawn, RitualRole role, out string reason, TargetInfo ritualTarget)
        {
            reason = null;
            Building_Bed bed = ritualTarget.Thing as Building_Bed;
            if (bed == null)
            {
                return false;
            }

            if (role == null)
            {
                return SoulboundAdoptionUtility.IsEligibleAdopter(pawn, bed) || SoulboundAdoptionUtility.HasEligibleAdopterForChild(pawn, bed);
            }
            if (role.id == "adopter")
            {
                if (SoulboundAdoptionUtility.IsEligibleAdopter(pawn, bed))
                {
                    return true;
                }
                if (pawn.genes?.GetGene(BernaelDefOf.BX_SoulboundAdoption) is Gene_SoulboundAdoption gene && gene.CooldownTicksRemaining > 0)
                {
                    reason = "BX_SoulboundAdoptionCooldown".Translate(gene.CooldownTicksRemaining.ToStringTicksToPeriod());
                }
                else
                {
                    reason = "BX_SoulboundAdoptionAdopterIneligible".Translate();
                }
                return false;
            }
            if (role.id == "child")
            {
                bool eligible = SoulboundAdoptionUtility.HasEligibleAdopterForChild(pawn, bed);
                if (!eligible)
                {
                    reason = "BX_SoulboundAdoptionChildIneligible".Translate();
                }
                return eligible;
            }
            return true;
        }

        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            Pawn child = assignments.FirstAssignedPawn("child");
            Building_Bed bed = target.Thing as Building_Bed;
            IntVec3? spot = child != null && bed != null ? RestUtility.GetBedSleepingSlotPosFor(child, bed) : null;
            return new LordJob_Ritual(target, ritual, obligation, def.stages, assignments, organizer, spot);
        }

        public override bool TargetStillAllowed(TargetInfo selectedTarget, LordJob_Ritual ritual)
        {
            if (selectedTarget.Thing is not Building_Bed bed)
            {
                return false;
            }
            Pawn adopter = ritual.assignments.FirstAssignedPawn("adopter");
            Pawn child = ritual.assignments.FirstAssignedPawn("child");
            return SoulboundAdoptionUtility.IsEligibleAdopter(adopter, bed) && SoulboundAdoptionUtility.IsUsableBedFor(child, adopter, bed);
        }
    }
}
