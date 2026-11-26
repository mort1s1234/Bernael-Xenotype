using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Bernael_Xenotype
{
    public sealed class CompProperties_ShadowMire_FlamePillar : CompProperties_ShadowMire
    {
        public CompProperties_ShadowMire_FlamePillar()
        {
            compClass = typeof(CompAbilityEffect_ShadowMire_FlamePillar);
        }
    }

    public sealed class CompAbilityEffect_ShadowMire_FlamePillar : CompAbilityEffect
    {
        private readonly List<Pawn> victims = new List<Pawn>();

        private ShadowMireDef FieldDef => ((CompProperties_ShadowMire)props).fieldDef;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!Valid(target, true)) return;
            base.Apply(target, dest);

            parent.pawn.Map.GetComponent<FlamePillarVisualMapComponent>()?.AddPillar(
                target.Cell,
                FieldDef.pillarRadius,
                ShadowMirePlume.SeedFor(parent.pawn.thingIDNumber, Find.TickManager.TicksGame, target.Cell, false)
            );

            victims.Clear();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, FieldDef.pillarRadius, true))
            {
                if (!cell.InBounds(parent.pawn.Map) || cell.Fogged(parent.pawn.Map)) continue;
                if (!GenSight.LineOfSight(target.Cell, cell, parent.pawn.Map, skipFirstCell: true)) continue;
                List<Thing> things = cell.GetThingList(parent.pawn.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn pawn && !pawn.Dead && !victims.Contains(pawn))
                        victims.Add(pawn);
                }
            }
            Thing instigator = parent.pawn;
            for (int i = 0; i < victims.Count; i++)
            {
                Pawn pawn = victims[i];
                if (pawn.Dead || pawn.Map != parent.pawn.Map) continue;
                pawn.TakeDamage(new DamageInfo(FieldDef.damageDef ?? DamageDefOf.Flame, FieldDef.pillarDamage, 0f, -1f, instigator));
                if (pawn.Map == parent.pawn.Map)
                    CursedFire.TryAttachFromMire(pawn, FieldDef.pillarFireSize, instigator);
            }
            victims.Clear();
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent?.pawn.Faction == Faction.OfPlayer)
                return false;
            return Valid(target);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (!Valid(target)) return;
            Map map = parent.pawn.Map;
            GenDraw.DrawRadiusRing(target.Cell, FieldDef.pillarRadius, Color.white,
                cell => ShadowMire.Reaches(target.Cell, cell, map));
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.IsValid && ShadowMire.OpenGround(target.Cell, parent.pawn.Map))
                return true;
            return false;
        }
    }

    [StaticConstructorOnStartup]
    public sealed class FlamePillarVisualMapComponent : MapComponent
    {
        private sealed class ActivePillar
        {
            public readonly IntVec3 cell;
            public readonly float radius;
            public readonly int seed;
            public readonly ShadowMirePlume plume = new ShadowMirePlume();
            public int age;

            public ActivePillar(IntVec3 cell, float radius, int seed)
            {
                this.cell = cell;
                this.radius = radius;
                this.seed = seed;
            }

            public void Dispose() => plume.Dispose();
        }

        private static readonly Material pillarMaterial;
        private static readonly bool paintPillar;

        private readonly List<ActivePillar> activePillars = new List<ActivePillar>();
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        static FlamePillarVisualMapComponent()
        {
            Shader pillar = DefDatabase<ShaderDef>.GetNamed("BX_ShadowMirePillar", errorOnFail: false)?.Shader;
            paintPillar = pillar == null || !pillar.isSupported;
            pillarMaterial = new Material(paintPillar ? ShaderDatabase.Transparent : pillar)
            {
                name = "Single flame pillar"
            };
        }

        public FlamePillarVisualMapComponent(Map map) : base(map)
        {
        }

        internal void AddPillar(IntVec3 cell, float radius, int seed)
        {
            activePillars.Add(new ActivePillar(cell, radius, seed));
        }

        public override void MapComponentTick()
        {
            for (int i = activePillars.Count - 1; i >= 0; i--)
            {
                ActivePillar pillar = activePillars[i];
                pillar.age++;
                if (pillar.age >= ShadowMirePlume.BurnTicks)
                {
                    pillar.Dispose();
                    activePillars.RemoveAt(i);
                }
            }
        }

        public override void MapComponentUpdate()
        {
            if (activePillars.Count == 0 || Find.CurrentMap != map) return;

            for (int i = 0; i < activePillars.Count; i++)
            {
                ActivePillar pillar = activePillars[i];
                if (pillar.age < 0 || pillar.age >= ShadowMirePlume.BurnTicks) continue;

                pillar.plume.Prepare(pillar.seed, pillar.age, pillar.radius, paintPillar);

                Vector3 at = pillar.cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverheadLow);
                at.z += pillar.plume.Height * (ShadowMirePlume.Top + ShadowMirePlume.Bottom) * 0.5f;

                properties.Clear();
                properties.SetTexture("_MainTex", paintPillar ? pillar.plume.Painted : pillar.plume.Profile);
                if (!paintPillar)
                {
                    properties.SetFloat("_Phase", pillar.age / 60f);
                    properties.SetFloat("_Seed", pillar.plume.Seed & 65535);
                }

                Vector3 size = new Vector3(
                    pillar.plume.Width * ShadowMirePlume.CanvasWidth,
                    1f,
                    pillar.plume.Height * (ShadowMirePlume.Top - ShadowMirePlume.Bottom)
                );

                Graphics.DrawMesh(
                    MeshPool.plane10,
                    Matrix4x4.TRS(at, Quaternion.identity, size),
                    pillarMaterial,
                    0,
                    null,
                    0,
                    properties,
                    ShadowCastingMode.Off,
                    false
                );
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ClearAllPillars();
        }

        public override void MapRemoved()
        {
            ClearAllPillars();
            base.MapRemoved();
        }

        private void ClearAllPillars()
        {
            for (int i = 0; i < activePillars.Count; i++)
                activePillars[i].Dispose();
            activePillars.Clear();
        }
    }
}
