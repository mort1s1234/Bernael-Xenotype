using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class Gene_DepravedHead : Gene
    {
        Gamecomponent_PsychicSight gameComp => Current.Game.GetGamePsychicSightComp();

        public override void PostAdd()
        {
            base.PostAdd();
            if (pawn.health == null) return;

            Gamecomponent_PsychicSight sightComp = gameComp;
            if (sightComp == null)
            {
                Log.Error("The psychic sight game component was missing, this isn't supposed to happen.");
            }
            else
            {
                if (sightComp.psychicSeers.NullOrEmpty())
                    sightComp.psychicSeers = new HashSet<Pawn>();
                sightComp.psychicSeers.Add(pawn);
            }


            foreach (BodyPartRecord bodyPart in pawn?.RaceProps?.body?.AllParts)
            {
                BodyPartDef bodyPartDef = bodyPart.def;
                if (!bodyPartDef.defName.ToLowerInvariant().Contains("eye")) continue;
                pawn.health.AddHediff(HediffMaker.MakeHediff(BernaelDefOf.BX_Blindness, pawn, bodyPart));
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (pawn.health == null) return;

            if (gameComp != null && !gameComp.psychicSeers.NullOrEmpty() && gameComp.psychicSeers.Contains(pawn))
                gameComp.psychicSeers.Remove(pawn);

            for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs[i];
                if (hediff.def != BernaelDefOf.BX_Blindness) continue;
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
