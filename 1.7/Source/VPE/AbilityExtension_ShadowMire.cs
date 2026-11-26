using RimWorld;
using RimWorld.Planet;
using Verse;
using VEF.Abilities;

namespace Bernael_Xenotype
{
    public sealed class AbilityExtension_ShadowMire : AbilityExtension_AbilityMod
    {
        public ShadowMireDef fieldDef;

        public override void Cast(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability)
        {
            base.Cast(targets, ability);
            if (targets == null || targets.Length == 0) return;

            GlobalTargetInfo target = targets[0];
            if (!Valid(targets, ability, throwMessages: true)) return;
            var mire = (ShadowMire)ThingMaker.MakeThing(fieldDef);
            mire.caster = ability.pawn;
            GenSpawn.Spawn(mire, target.Cell, ability.pawn.Map);
        }

        public override bool Valid(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability, bool throwMessages = false)
        {
            if (targets == null || targets.Length == 0) return false;

            if (targets[0].Cell.IsValid && targets[0].Cell.InBounds(ability.pawn.Map) && !targets[0].HasThing &&
                ShadowMire.OpenGround(targets[0].Cell, ability.pawn.Map)) return true;
            if (throwMessages)
                Messages.Message("BX_ShadowMireNeedsGround".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }
}
