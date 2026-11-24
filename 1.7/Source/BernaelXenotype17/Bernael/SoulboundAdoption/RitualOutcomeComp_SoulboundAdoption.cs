using RimWorld;

namespace Bernael_Xenotype
{
    public class RitualOutcomeComp_SoulboundAdoption : RitualOutcomeComp
    {
        public override bool DataRequired => false;

        public override bool Applies(LordJob_Ritual ritual)
        {
            return false;
        }
    }
}
