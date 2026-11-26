using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    public sealed class MeleeSwingState
    {
        public int StartTick = -1;
        public int DurationTicks = 12;

        public bool IsActive(Pawn pawn, int currentTick) =>
            pawn is { Destroyed: false, Dead: false, Downed: false } &&
            StartTick >= 0 && DurationTicks > 0 &&
            currentTick >= StartTick && currentTick <= StartTick + DurationTicks;

        public float Progress(int currentTick) =>
            DurationTicks <= 0 ? 1f : Mathf.Clamp01((currentTick - StartTick) / (float)DurationTicks);

        public void Trigger(int currentTick, int durationTicks)
        {
            StartTick = currentTick;
            DurationTicks = Math.Max(1, durationTicks);
        }
    }

    public sealed class AbilityCastRaiseState
    {
        private const int SafetyBufferTicks = 60;
        public bool IsRaising;
        public int StartTick = -1;
        public int ExpectedEndTick = -1;

        public void Begin(int currentTick, float warmupSeconds)
        {
            IsRaising = true;
            StartTick = currentTick;
            int warmupTicks = warmupSeconds > 0f ? Mathf.Max(1, warmupSeconds.SecondsToTicks()) : 1;
            ExpectedEndTick = currentTick + warmupTicks + SafetyBufferTicks;
        }

        public void Complete()
        {
            IsRaising = false;
            StartTick = -1;
            ExpectedEndTick = -1;
        }

        public bool IsActive(Pawn pawn, int currentTick)
        {
            if (!IsRaising || pawn is not { Destroyed: false, Dead: false, Downed: false, Spawned: true })
                return false;

            if (StartTick < 0 || currentTick < StartTick)
                return false;

            if ((ExpectedEndTick >= 0 && currentTick > ExpectedEndTick) ||
                (currentTick > StartTick && pawn.stances is { FullBodyBusy: false, curStance: not Stance_Warmup }))
            {
                Complete();
                return false;
            }
            return true;
        }

        public float Progress(int currentTick)
        {
            if (!IsRaising || StartTick < 0) return 1f;
            int warmupDuration = Math.Max(1, ExpectedEndTick - StartTick - SafetyBufferTicks);
            return Mathf.Clamp01((currentTick - StartTick) / (float)warmupDuration);
        }
    }

    public sealed class HandAnimState
    {
        public readonly MeleeSwingState Swing = new();
        public readonly AbilityCastRaiseState CastRaise = new();
    }

    public static class HandAnimTracker
    {
        private static readonly ConditionalWeakTable<Pawn, HandAnimState> Table = new();

        public static HandAnimState GetOrCreate(Pawn pawn) => Table.GetValue(pawn, _ => new HandAnimState());

        public static bool TryGet(Pawn pawn, out HandAnimState state) => Table.TryGetValue(pawn, out state);
    }
}
