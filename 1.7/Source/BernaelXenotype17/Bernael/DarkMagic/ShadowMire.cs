using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    public sealed class ShadowMireDef : ThingDef
    {
        public float radius;
        public int durationTicks;
        public float pillarRadius;
        public float pillarDamage;
        public float pillarFireSize = 0.1f;
        public DamageDef damageDef;
        public HediffDef slowDef;
    }

    public class CompProperties_ShadowMire : CompProperties_AbilityEffect
    {
        public ShadowMireDef fieldDef;

        public CompProperties_ShadowMire() { compClass = typeof(CompAbilityEffect_ShadowMire); }
    }

    public sealed class CompAbilityEffect_ShadowMire : CompAbilityEffect
    {
        private ShadowMireDef FieldDef => ((CompProperties_ShadowMire)props).fieldDef;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!Valid(target, true)) return;
            base.Apply(target, dest);
            var mire = (ShadowMire)ThingMaker.MakeThing(FieldDef);
            mire.caster = parent.pawn;
            GenSpawn.Spawn(mire, target.Cell, parent.pawn.Map);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (!Valid(target)) return;
            Map map = parent.pawn.Map;
            GenDraw.DrawRadiusRing(target.Cell, FieldDef.radius, Color.white,
                cell => ShadowMire.Reaches(target.Cell, cell, map));
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.IsValid && !target.HasThing && parent.pawn.Spawned &&
                ShadowMire.OpenGround(target.Cell, parent.pawn.Map)) return true;
            if (throwMessages)
                Messages.Message("BX_ShadowMireNeedsGround".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    public sealed class ShadowMire : Thing
    {
        internal const int WaveTicks = 80;
        internal const int WarningTicks = 24;
        internal const int SecondPillarDelay = 16;
        private const int SlowRefreshTicks = 10;

        public Pawn caster;
        private int age;
        private IntVec3 firstPillar = IntVec3.Invalid;
        private IntVec3 secondPillar = IntVec3.Invalid;
        private readonly List<IntVec3> cells = new List<IntVec3>();
        private readonly List<IntVec3> scratchCells = new List<IntVec3>();
        private MireGround[] ground;
        private int groundWidth;
        private readonly List<Pawn> victims = new List<Pawn>();
        private ShadowMireVisuals visuals;
        private bool groundChanged = true;
        private ShadowMireDef Settings => (ShadowMireDef)def;

        internal static bool HasAir(IntVec3 cell, Map map) => cell.GetVacuum(map) < 0.5f;

        public static bool OpenGround(IntVec3 cell, Map map) =>
            cell.InBounds(map) && !cell.Fogged(map) && cell.Walkable(map) && cell.CanBeSeenOverFast(map) &&
            !cell.GetTerrain(map).IsWater && HasAir(cell, map);

        private static MireGround GroundAt(IntVec3 center, IntVec3 cell, Map map)
        {
            if (!OpenGround(cell, map)) return MireGround.Blocked;
            return GenSight.LineOfSight(center, cell, map, skipFirstCell: true) ? MireGround.Wet : MireGround.Sheltered;
        }

        internal static bool Reaches(IntVec3 center, IntVec3 cell, Map map) => GroundAt(center, cell, map) == MireGround.Wet;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad) return;
            RefreshCells();
            StartWave();
            SlowOccupants();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= Settings.durationTicks || (age % SlowRefreshTicks == 0 && !OpenGround(Position, Map)))
            {
                Destroy();
                return;
            }
            if (age % SlowRefreshTicks == 0 || cells.Count == 0)
            {
                RefreshCells();
                SlowOccupants();
            }
            int phase = age % WaveTicks;
            if (phase == 0) StartWave();
            if (phase == WarningTicks) Strike(firstPillar);
            if (phase == WarningTicks + SecondPillarDelay) Strike(secondPillar);
        }

        private void RefreshCells()
        {
            cells.Clear();
            // The two outer rows supply samples where the bank meets the circular edge.
            float sampleRadius = Settings.radius + 2f;
            int half = Mathf.CeilToInt(sampleRadius);
            groundWidth = 2 * half + 1;
            if (ground == null || ground.Length != groundWidth * groundWidth)
            {
                ground = new MireGround[groundWidth * groundWidth];
                groundChanged = true;
            }
            int count = GenRadial.NumCellsInRadius(sampleRadius);
            for (int i = 0; i < count; i++)
            {
                IntVec3 offset = GenRadial.RadialPattern[i];
                IntVec3 cell = Position + offset;
                MireGround state = GroundAt(Position, cell, Map);
                int index = (offset.z + half) * groundWidth + offset.x + half;
                if (state != ground[index])
                {
                    ground[index] = state;
                    groundChanged = true;
                }
                if (state == MireGround.Wet && cell.InHorDistOf(Position, Settings.radius)) cells.Add(cell);
            }
        }

        private void SlowOccupants()
        {
            foreach (IntVec3 cell in cells)
            {
                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is not Pawn pawn || pawn.Dead) continue;
                    Hediff slow = pawn.health.GetOrAddHediff(Settings.slowDef);
                    slow.TryGetComp<HediffComp_Disappears>().ResetElapsedTicks();
                }
            }
        }

        private void StartWave()
        {
            int seed = Gen.HashCombineInt(thingIDNumber, age);
            scratchCells.Clear();
            foreach (IntVec3 cell in cells)
                if (cell.GetFirstPawn(Map) != null) scratchCells.Add(cell);
            firstPillar = age == 0 ? Position : Pick(scratchCells.Count > 0 ? scratchCells : cells, seed);

            scratchCells.Clear();
            foreach (IntVec3 cell in cells)
                if (!cell.InHorDistOf(firstPillar, 2f * Settings.pillarRadius)) scratchCells.Add(cell);
            secondPillar = Pick(scratchCells, Gen.HashCombineInt(seed, 1));
        }

        private static IntVec3 Pick(List<IntVec3> candidates, int seed) => candidates.Count == 0
            ? IntVec3.Invalid : candidates[Rand.RangeSeeded(0, candidates.Count, seed)];

        private void Strike(IntVec3 pillar)
        {
            if (!pillar.IsValid || !OpenGround(Position, Map) || !Reaches(Position, pillar, Map)) return;
            victims.Clear();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pillar, Settings.pillarRadius, true))
            {
                if (!cell.InHorDistOf(Position, Settings.radius) || !Reaches(Position, cell, Map) ||
                    !GenSight.LineOfSight(pillar, cell, Map, skipFirstCell: true)) continue;
                foreach (Thing thing in cell.GetThingList(Map))
                    if (thing is Pawn pawn && !pawn.Dead && !victims.Contains(pawn)) victims.Add(pawn);
            }
            // Death and downing can change the thing grid during damage resolution.
            Thing instigator = caster ?? (Thing)this;
            foreach (Pawn pawn in victims)
            {
                if (pawn.Dead || pawn.Map != Map) continue;
                pawn.TakeDamage(new DamageInfo(Settings.damageDef, Settings.pillarDamage, 0f, -1f, instigator));
                if (pawn.Map == Map)
                    CursedFire.TryAttachFromMire(pawn, Settings.pillarFireSize, instigator);
            }
            victims.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref age, "mireAge");
            Scribe_Values.Look(ref firstPillar, "firstPillar", IntVec3.Invalid);
            Scribe_Values.Look(ref secondPillar, "secondPillar", IntVec3.Invalid);
        }

        public override string GetInspectString() =>
            "BX_ShadowMireRemaining".Translate((Settings.durationTicks - age).ToStringSecondsFromTicks("F1"));

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int margin = Mathf.CeilToInt(Settings.radius + ShadowMireVisuals.MaxPillarHeight);
            if (!Find.CameraDriver.CurrentViewRect.ExpandedBy(margin).Contains(Position))
                return;
            if (cells.Count == 0) RefreshCells();
            visuals ??= new ShadowMireVisuals();
            if (groundChanged)
            {
                visuals.SetGround(ground, groundWidth, Settings.radius);
                groundChanged = false;
            }
            visuals.Draw(drawLoc, Position, Settings, age, thingIDNumber,
                cells.Contains(firstPillar) ? firstPillar : IntVec3.Invalid,
                cells.Contains(secondPillar) ? secondPillar : IntVec3.Invalid);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            visuals?.Dispose();
            visuals = null;
            groundChanged = true;
            base.DeSpawn(mode);
        }
    }
}
