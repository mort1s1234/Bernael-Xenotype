using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class ModExt_SeverityBasedOnNeed : DefModExtension
    {
        public class SeverityModifierByNeed
        {
            public NeedDef needDef;
            public SimpleCurve severity;
        }

        public SeverityModifierByNeed severityFromNeed;
    }

    public class Hediff_SeverityBasedOnNeed : HediffWithComps
    {
        ModExt_SeverityBasedOnNeed ModExt => this.def.GetModExtension<ModExt_SeverityBasedOnNeed>();
        Need need => pawn?.needs?.TryGetNeed(ModExt.severityFromNeed.needDef);
        public override void Tick()
        {
            base.Tick();
            if (!pawn.IsHashIntervalTick(420) || need == null) return;
            float Check = ModExt.severityFromNeed.severity.Evaluate(need.CurLevel);
            //Log.Error($"{Check}");
            Severity = Check;
        }
    }
}
