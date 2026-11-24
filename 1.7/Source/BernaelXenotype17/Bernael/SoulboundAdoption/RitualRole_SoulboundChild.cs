using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class RitualRole_SoulboundChild : RitualRole
    {
        public override bool AppliesToPawn(Pawn pawn, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (!SoulboundAdoptionUtility.IsEligibleChild(pawn))
            {
                if (!skipReason)
                {
                    reason = "BX_SoulboundAdoptionChildIneligible".Translate();
                }
                return false;
            }
            if (selectedTarget.Thing is not Building_Bed bed)
            {
                if (!skipReason)
                {
                    reason = "BX_SoulboundAdoptionChildNoBed".Translate();
                }
                return false;
            }
            Pawn adopter = assignments?.FirstAssignedPawn("adopter");
            bool bedIsUsable = adopter != null
                ? SoulboundAdoptionUtility.IsEligibleAdopter(adopter, bed) && SoulboundAdoptionUtility.IsUsableBedFor(pawn, adopter, bed)
                : SoulboundAdoptionUtility.HasEligibleAdopterForChild(pawn, bed);
            if (!bedIsUsable)
            {
                if (!skipReason)
                {
                    reason = "BX_SoulboundAdoptionChildNoBed".Translate();
                }
                return false;
            }
            return true;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn pawn = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
