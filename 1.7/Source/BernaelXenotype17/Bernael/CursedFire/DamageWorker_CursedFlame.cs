using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    public class DamageWorker_CursedFlame : DamageWorker_AddInjury
    {
        private static readonly ConditionalWeakTable<Explosion, object> ShockwaveTriggered = new();
        private static readonly object TriggeredMarker = new();

        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn pawn = victim as Pawn;
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
                Find.TickManager.slower.SignalForceNormalSpeedShort();
            Map map = victim.Map;
            DamageResult damageResult = base.Apply(dinfo, victim);
            if (map == null) return damageResult;
            if (!damageResult.deflected && !dinfo.InstantPermanentInjury &&
                pawn != null && !pawn.Dead && !pawn.Destroyed &&
                Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(victim)))
            {
                CursedFire.TryAttachFromMire(pawn, Rand.Range(0.15f, 0.25f), dinfo.Instigator);
            }
            if (victim.Destroyed && pawn == null)
            {
                foreach (IntVec3 item in victim.OccupiedRect())
                    FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_Ash);
            }
            return damageResult;
        }

        public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
		{
		    base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
		    if (def == BernaelDefOf.BX_CursedFlame && explosion?.Map != null)
		    {
		        if (!ShockwaveTriggered.TryGetValue(explosion, out _))
		        {
		            ShockwaveTriggered.Add(explosion, TriggeredMarker);
		            DireOrbMapComponent.AddShockwave(
		                explosion.Map,
		                explosion.Position.ToVector3Shifted(),
		                explosion.Position,
		                explosion.radius
		            );
		        }
		        CursedFire.TryStartCursedFireIn(
		            explosion.Map,
		            c,
		            Rand.Range(0.1f, 0.3f),
		            explosion.instigator,
		            explosion.instigator?.Faction,
		            null,
		            CursedFire.CalcRadialGrowDelayTicks(explosion.Position, c),
		            checkCellRules: true
		        );
		    }
		}
    }
}
