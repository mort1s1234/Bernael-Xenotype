using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class ReaperModExt : DefModExtension
    {
        public float killRefillPct = 1;
        public int decayTickInterval = 60;
    }

    public class Hediff_ReapersEcstasy : HediffWithComps
    {
        public ReaperModExt ModExt => this.def.GetModExtension<ReaperModExt>();

        public override void Tick()
        {
            base.Tick();
            if (!pawn.IsHashIntervalTick(ModExt.decayTickInterval) || this.Severity <= 1) return;
            this.Severity -= 1;
        }

        public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            base.Notify_KilledPawn(victim, dinfo);
            if (ModExt == null || !victim.HostileTo(pawn.Faction) || pawn?.genes?.GetGene(BernaelDefOf.BX_SoulStarved) == null) return;
            Gene_Soul gene_Soul = (Gene_Soul)pawn.genes.GetGene(BernaelDefOf.BX_SoulStarved);

            gene_Soul.ValuePercent += ModExt.killRefillPct;
            this.Severity += 1;
        }

        public override string LabelInBrackets => base.LabelInBrackets + (Severity != this.def.maxSeverity ? "BX_ReaperEcstasyKillCount".ToString().Translate(this.Severity - 1, this.def.maxSeverity) : "BX_ReaperEcstasyKillCountMax".ToString().Translate());
    }
}
