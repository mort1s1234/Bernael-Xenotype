using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class CompProperties_CreateArcanaStone : CompProperties_AbilityEffect
    {
        public CompProperties_CreateArcanaStone() => compClass = typeof(CompProperties_CreateArcanaStone);

        public ThingDef stoneCreated;
    }
}
