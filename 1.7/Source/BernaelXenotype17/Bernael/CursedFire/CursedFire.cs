using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Bernael_Xenotype
{
    [StaticConstructorOnStartup]
    public class CursedFire : Fire
    {
        private enum FireSource { Ordinary, DireOrb, ShadowMire }

        private int cursedAge;
        private Sustainer sustainer;
        private FireSource source;
        private Faction sourceFaction;
        private Pawn struck;
        private static readonly MaterialPropertyBlock fireProperties = new MaterialPropertyBlock();

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (source == FireSource.Ordinary || !DarkMagicAssets.Ready())
            {
                base.DrawAt(drawLoc, flip);
                return;
            }
            // Keep small fires readable without increasing their damage, growth or lifetime.
            // Each burning cell gets its own size, lean and facing so a field of them never reads as a grid.
            int seed = thingIDNumber % 997;
            float r1 = (seed * 31 % 97) / 96f, r2 = (seed * 53 % 89) / 88f, r3 = (seed * 71 % 83) / 82f;
            float scale = parent != null ? 1f : r1 < 0.35f ? Mathf.Lerp(1.05f, 1.25f, r2)
                : r1 < 0.75f ? Mathf.Lerp(0.78f, 0.92f, r2) : Mathf.Lerp(0.5f, 0.62f, r2);
            float size = (0.55f + fireSize * 0.22f) * scale;
            if (parent == null)
            {
                // The entity stays at its exact gameplay cell. Only its flame leans within it.
                drawLoc.x += (r2 - 0.5f) * 0.76f;
                drawLoc.z += (r3 - 0.5f) * 0.6f;
            }
            float grow = growStartTick < 0 ? 1f : Mathf.Clamp01((Find.TickManager.TicksGame - growStartTick) / 22f);
            fireProperties.Clear();
            fireProperties.SetFloat("_Mode", 3f);
            fireProperties.SetFloat("_Phase", Find.TickManager.TicksGame / 60f);
            fireProperties.SetFloat("_Seed", seed * 0.13f);
            fireProperties.SetFloat("_Opacity", 1f);
            fireProperties.SetFloat("_Grow", grow);
            fireProperties.SetFloat("_Flip", r3 > 0.5f ? 1f : 0f);
            float height = size * Mathf.Lerp(1.15f, 1.5f, r3);
            drawLoc.z += height * 0.5f - 0.18f * size;
            DarkMagicAssets.Draw(DarkMagicAssets.Orb, drawLoc, Quaternion.identity,
                new Vector3(size, 1f, height), fireProperties);
        }

        public void ConfigureDireOrb(Pawn caster, Faction faction, Pawn directTarget, int growDelayTicks = 0)
        {
            source = FireSource.DireOrb;
            instigator = caster;
            sourceFaction = faction;
            struck = directTarget;
            growStartTick = Find.TickManager.TicksGame + growDelayTicks;
        }

        // Visual only: when the burst lit this fire. Loaded and merged fires are already fully grown.
        private int growStartTick = -1;

        private static bool CanBurn(Pawn pawn, FireSource source)
        {
            if (pawn.Dead) return false;
            return source switch
            {
                FireSource.DireOrb => !pawn.Downed,
                FireSource.ShadowMire => ShadowMire.HasAir(pawn.Position, pawn.Map),
                _ => true
            };
        }

        public override string Label
        {
            get
            {
                if (parent != null)
                {
                    return "BX_CursedFireOn".Translate(parent.LabelCap, parent);
                }
                return def.label;
            }
        }

        public override string InspectStringAddon => "Burning".Translate() + " (" + "BX_FireSizeLower".Translate((fireSize * 100f).ToString("F0")) + ")";

        public override int UpdateRateTicks => 15;

        public override void ExposeData()
        {
            base.ExposeData();
            // Fire already scribes fireSize/instigator and its own ticksSinceSpawn.
            // A separate key prevents the base and derived ages from shadowing each other on repeated loads.
            Scribe_Values.Look(ref cursedAge, "cursedAge", 0, forceSave: true);
            if (Scribe.mode == LoadSaveMode.LoadingVars && Scribe.loader.curXmlParent?["cursedAge"] == null)
            {
                var legacy = Scribe.loader.curXmlParent?.SelectNodes("ticksSinceSpawn");
                if (legacy != null && legacy.Count > 0 && int.TryParse(legacy[legacy.Count - 1].InnerText, out int age))
                    cursedAge = age;
            }
            Scribe_Values.Look(ref source, "cursedFireSource");
            if (Scribe.mode == LoadSaveMode.LoadingVars && source == FireSource.Ordinary)
            {
                bool fromDireOrb = false;
                Scribe_Values.Look(ref fromDireOrb, "fromDireOrb");
                if (fromDireOrb) source = FireSource.DireOrb;
            }
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
            Scribe_References.Look(ref struck, "struck");
        }
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (parent != null)
                map.events.ThingDespawned += NotifyThingDespawned;
            else if (!respawningAfterLoad && Spawned)
                MergeExistingFires(map);
        }

        private void NotifyThingDespawned(Thing thing)
        {
            if (thing == parent) Destroy();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (parent != null) Map.events.ThingDespawned -= NotifyThingDespawned;
            // Sustainers may be shared by nearby fires; remove only this reporter.
            if (sustainer != null)
            {
                if (sustainer.externalParams.sizeAggregator == null)
                    sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
                sustainer.externalParams.sizeAggregator.RemoveReporter(this);
            }
            base.DeSpawn(mode);
        }

        protected override void Tick()
        {
            // This class owns its sound rather than the base Fire's private sustainer.
            if (sustainer != null && !sustainer.Ended) sustainer.Maintain();
        }

        protected override void TickInterval(int delta)
        {
            if (parent is Pawn burning && (burning.Map != Map || !CanBurn(burning, source)))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            if (parent == null && Spawned)
            {
                MergeExistingFires(Map);
            }
            cursedAge += delta;
            float maxLifetime = 600f + (fireSize * 3000f); 
            if (cursedAge >= maxLifetime)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            if ((sustainer == null || sustainer.Ended) && !Position.Fogged(Map))
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(Position, Map), MaintenanceType.PerTick);
                sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, SoundDefOf.FireBurning, info);
            }
            if (parent == null && Spawned)
            {
                Pawn steppingPawn = Map.thingGrid.ThingAt<Pawn>(Position);
                if (steppingPawn != null)
                    TryAttach(steppingPawn, fireSize, instigator, source, Position, Map, sourceFaction, struck);
            }
            if (parent != null && this.IsHashIntervalTick(150, delta))
            {
                DoCursedDamage(parent);
            }
            if (Spawned)
            {
                float coldPower = fireSize * -80f;
                GenTemperature.PushHeat(Position, Map, coldPower);
                fireSize += 0.0002f * delta;
                if (fireSize > MaxFireSize) 
                {
                    fireSize = MaxFireSize;
                }
            }
        }

        private bool CanMerge(Fire other)
        {
            if (other is not CursedFire cursed) return source == FireSource.Ordinary;
            if (source != cursed.source) return false;
            return source == FireSource.Ordinary || (instigator == cursed.instigator && sourceFaction == cursed.sourceFaction && struck == cursed.struck);
        }

        private void MergeExistingFires(Map map)
        {
            bool hasMerged = false;
            List<Thing> thingList = Position.GetThingList(map);
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing thing = thingList[i];
                if (thing is Fire otherFire && otherFire != this && otherFire.parent == null && CanMerge(otherFire))
                {
                    fireSize += otherFire.fireSize;
                    if (fireSize > MaxFireSize)
                    {
                        fireSize = MaxFireSize;
                    }
                    otherFire.Destroy(DestroyMode.Vanish);
                    hasMerged = true;
                }
            }
            if (hasMerged)
            {
                cursedAge = 0;
            }
        }

        internal static void TryAttachFromMire(Pawn pawn, float fireSize, Thing instigator) =>
            TryAttach(pawn, fireSize, instigator, FireSource.ShadowMire, pawn.Position, pawn.Map);

        public static void TryAttachCursedFire(Thing t, float fireSize, Thing instigator, bool fromDireOrb, Faction sourceFaction, Pawn struck, IntVec3 position, Map map) =>
            TryAttach(t, fireSize, instigator, fromDireOrb ? FireSource.DireOrb : FireSource.Ordinary,
                position, map, sourceFaction, struck);

        private static void TryAttach(Thing t, float fireSize, Thing instigator, FireSource source,
            IntVec3 position, Map map, Faction sourceFaction = null, Pawn struck = null)
        {
            if (t is not Pawn pawn || !pawn.Spawned || pawn.Map != map || !CanBurn(pawn, source) ||
                !pawn.TryGetComp(out CompAttachBase attachments) || attachments.HasAttachment(BernaelDefOf.BX_CursedFire)) return;

            CursedFire pawnFire = (CursedFire)ThingMaker.MakeThing(BernaelDefOf.BX_CursedFire);
            pawnFire.fireSize = fireSize;
            pawnFire.instigator = instigator;
            pawnFire.source = source;
            pawnFire.sourceFaction = sourceFaction;
            pawnFire.struck = struck;
            pawnFire.AttachTo(t);
            GenSpawn.Spawn(pawnFire, position, map, Rot4.North);
            if (pawnFire.TryGetComp<CompGlower>(out var glower) && map != null)
            {
                map.glowGrid.DeRegisterGlower(glower);
                map.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Things);
            }
        }

        private void DoCursedDamage(Thing target)
        {
            if (source == FireSource.DireOrb && (target is not Pawn victim || !CanBurn(victim, source))) return;
            DamageDef damage = source == FireSource.Ordinary ? DamageDefOf.Frostbite : BernaelDefOf.BX_CursedFlame;
            int damageAmount = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * fireSize, 0.0125f, 0.05f) * 150f);
            if (damageAmount < 1)
            {
                damageAmount = 1;
            }
            if (target is Pawn pawn)
            {
                DamageInfo dinfo = new(damage, damageAmount, 0f, -1f, instigator ?? this);
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                pawn.TakeDamage(dinfo);
                if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out var apparelResult))
                {
                    apparelResult.TakeDamage(new DamageInfo(damage, damageAmount, 0f, -1f, instigator ?? this));
                }
            }
            else
            {
                target.TakeDamage(new DamageInfo(damage, damageAmount, 0f, -1f, instigator ?? this));
            }
        }

        public static int CalcRadialGrowDelayTicks(IntVec3 origin, IntVec3 cell, int baseDelay = 6, float ticksPerCell = 3f)
            => baseDelay + Mathf.RoundToInt(cell.DistanceTo(origin) * ticksPerCell);

        public static bool TryStartCursedFireIn(Map map, IntVec3 cell, float fireSize, Thing instigator,
            Faction faction = null, Pawn struck = null, int growDelayTicks = 0, bool checkCellRules = true)
        {
            if (map == null || !cell.InBounds(map)) return false;
            if (checkCellRules)
            {
                if (!cell.Walkable(map) || cell.GetTerrain(map).IsWater) return false;
                bool blocked = cell.GetThingList(map).Any(t => t.FireBulwark ||
                    (t.def.category == ThingCategory.Filth && !t.def.filth.allowsFire));
                if (blocked) return false;
            }
            ThingDef fireDef = BernaelDefOf.BX_CursedFire ?? DefDatabase<ThingDef>.GetNamed("BX_CursedFire");
            CursedFire fire = (CursedFire)ThingMaker.MakeThing(fireDef);
            fire.fireSize = fireSize;
            fire.ConfigureDireOrb(instigator as Pawn, faction ?? instigator?.Faction, struck, growDelayTicks);
            fire.instigator = instigator;
            GenSpawn.Spawn(fire, cell, map, Rot4.North);
            return true;
        }
    }
}
