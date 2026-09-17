using RimWorld;
using Verse;
namespace Bernael_Xenotype
{
    public class HediffComp_SeverityFromSoul : HediffComp
    {
        public HediffCompProperties_SeverityFromSoul Props => props as HediffCompProperties_SeverityFromSoul;

        public override bool CompShouldRemove
        {
            get
            {
                Pawn_GeneTracker genes = Pawn.genes;
                if (genes?.GetFirstGeneOfType<Gene_Soul>() == null)
                {
                    Log.Message(">??");
                    return true;
                }
                return false;
            }
        }

        private Gene_Soul Soul
        {
            get
            {
                if (cachedSoulGene == null)
                {
                    cachedSoulGene = Pawn.genes.GetFirstGeneOfType<Gene_Soul>();
                }
                return cachedSoulGene;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Soul != null)
            {
                Log.Message(severityAdjustment);
                severityAdjustment += (Soul.Value > 0f ? Props.severityPerHourSoul : Props.severityPerHourEmpty) / 2500f;
                Log.Message(severityAdjustment);
            }
        }

        private Gene_Soul cachedSoulGene;
    }
}