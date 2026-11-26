using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Bernael_Xenotype
{
    public class DeathActionWorker_SmallCursedFlameExplosion : DeathActionWorker
    {
    	public override RulePackDef DeathRules => RulePackDefOf.Transition_DiedExplosive;

    	public override bool DangerousInMelee => true;

    	public override void PawnDied(Corpse corpse, Lord prevLord)
    	{
    		GenExplosion.DoExplosion(corpse.Position, corpse.Map, 1.9f, BernaelDefOf.BX_CursedFlame, corpse.InnerPawn);
    	}
    }
}
