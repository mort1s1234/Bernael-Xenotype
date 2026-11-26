using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace Bernael_Xenotype
{
    /// <summary>
    /// A few hours before the attack, a baby or child gets a thought that someone unfamiliar is nearby,
    /// which applies a mood debuff.<br/>
    /// If the player notices this and starts protecting/hiding the child, the Bernael will attack earlier than scheduled.
    /// </summary>
    public class IncidentWorker_MakeInitialBogeyman : IncidentWorker_MakeGameCondition
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return HasBabyOrChildVictim(map) && RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Hostile);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
	    {
	    	GameConditionManager gameConditionManager = parms.target.GameConditionManager;
	    	GameCondition cond = GameConditionMaker.MakeConditionPermanent(GetGameConditionDef(parms));
	    	gameConditionManager.RegisterCondition(cond);
	    	return true;
	    }

        private static bool HasBabyOrChildVictim(Map map)
        {
            return map.mapPawns.FreeColonistsSpawned.Any(p => p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Child());
        }
    }

    /// <summary>
    /// Right in the room with the child, accompanied by landing particles and smoke (Wing-Burst),
    /// a Bernael appears alongside a few weak demons.<br/>
    /// The Bernael stashes the child in its inventory.<br/>
    /// The Bernael makes a mad dash for the edge of the map, utilizing its abilities and the Fold Shift gene.<br/>
    /// Its goal is to successfully escape, so it will use all available means to achieve this.<br/>
    /// To create a distraction, a raid could spawn to draw away defenses.<br/>
    /// If the Bernael manages to escape, the child could later reappear as an adult raider who has become a Bernael of the Cult of Caligo.
    /// </summary>
    public class IncidentWorker_Bogeyman : IncidentWorker
    {
        private const float SmokeRadius = 3.5f;
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
		    IntVec3 result = parms.spawnCenter;
		    if (!result.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out result, map, CellFinder.EdgeRoadChance_Hostile))
		    {
		    	return false;
		    }

            IntVec3 spawnCell = TryFindSpawnCell(map);
            if (!spawnCell.IsValid) return false;

            PawnKindDef bogeymanKind = BernaelDefOf.BX_Caligo_Bogeyman; // PawnKindDef
            Faction faction = FactionUtility.DefaultFactionFrom(bogeymanKind.defaultFactionDef) ?? Find.FactionManager.RandomEnemyFaction();

            Pawn bogeyman = PawnGenerator.GeneratePawn(bogeymanKind, faction);
            GenSpawn.Spawn(bogeyman, spawnCell, map, WipeMode.Vanish);

            FleckMaker.Static(spawnCell, map, FleckDefOf.PsycastSkipInnerExit);
            FleckMaker.Static(spawnCell, map, FleckDefOf.PsycastSkipOuterRingExit);
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(spawnCell, map));
            // The dark landing cloud replaces the explosion's own cell flashes; damage, gas and smoke are unchanged.
            GenExplosion.DoExplosion(
                center: spawnCell,
                map: map,
                radius: SmokeRadius,
                damType: DamageDefOf.Smoke,
                instigator: bogeyman,
                damAmount: 0,
                armorPenetration: -1f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: BernaelDefOf.BX_WingBurstSmoke,
                postExplosionSpawnChance: 1f,
                postExplosionSpawnThingCount: 1,
                postExplosionGasType: GasType.BlindSmoke,
                doVisualEffects: false
            );

            LordMaker.MakeNewLord(faction, new LordJob_Bogeyman(), map, new[] { bogeyman });

            SendStandardLetter(parms, bogeyman, bogeyman.Named("PAWN"));
            return true;
        }

        private static IntVec3 TryFindSpawnCell(Map map)
        {
            Pawn targetChild = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Child());
            if (targetChild != null)
            {
                return CellFinder.RandomClosewalkCellNear(targetChild.Position, map, 2);
            }
            Building_Bed babyBed = map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>().FirstOrDefault(b => (b.def.building.bed_crib || b.def.building.bed_maxBodySize < 1f) && b.AnyUnownedSleepingSlot);
            if (babyBed != null)
            {
                return CellFinder.RandomClosewalkCellNear(babyBed.Position, map, 2);
            }
            return IntVec3.Invalid;
        }
    }
}
