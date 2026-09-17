using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
namespace Bernael_Xenotype
{
    public class WorkGiver_Warden_DeliverSoulGem : WorkGiver_Warden
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ModsConfig.BiotechActive)
            {
                return null;
            }
            if (!ShouldTakeCareOfPrisoner(pawn, t))
            {
                return null;
            }
            Pawn prisoner = (Pawn)t;
            if (!prisoner.guest.CanBeBroughtFood || !prisoner.Position.IsInPrisonCell(prisoner.Map))
            {
                return null;
            }
            if (WardenFeedUtility.ShouldBeFed(prisoner))
            {
                return null;
            }
            Pawn_GeneTracker genes = prisoner.genes;
            Gene_Soul gene_Soul = ((genes != null) ? genes.GetGene(BernaelDefOf.BX_SoulStarved) : null) as Gene_Soul;
            if (gene_Soul == null)
            {
                return null;
            }
            if (gene_Soul.soulBasicGemsAllowed)
            {
                if (!gene_Soul.ShouldConsumeSoulNow())
                {
                    return null;
                }
                if (SoulGemAlreadyAvailableFor(prisoner))
                {
                    return null;
                }
                Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(BernaelDefOf.BX_BottledSoul), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, pack => !pack.IsForbidden(pawn) && pawn.CanReserve(pack) && pack.GetRoom() != prisoner.GetRoom());
                if (thing == null)
                {
                    return null;
                }
                Job job = JobMaker.MakeJob(JobDefOf.DeliverFood, thing, prisoner);
                job.count = 1;
                job.targetC = RCellFinder.SpotToChewStandingNear(prisoner, thing);
                return job;
            }
            return null;

        }

        private bool SoulGemAlreadyAvailableFor(Pawn prisoner)
        {
            if (prisoner.carryTracker.CarriedCount(BernaelDefOf.BX_BottledSoul) > 0)
            {
                return true;
            }
            if (prisoner.inventory.Count(BernaelDefOf.BX_BottledSoul) > 0)
            {
                return true;
            }
            Room room = prisoner.GetRoom();
            if (room != null)
            {
                List<Region> regions = room.Regions;
                for (int i = 0; i < regions.Count; i++)
                {
                    if (regions[i].ListerThings.ThingsOfDef(BernaelDefOf.BX_BottledSoul).Count > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}