using Verse;

namespace Bernael_Xenotype
{
    public class Gene_SoulboundAdoption : Gene
    {
        public const int CooldownTicks = 3600000;

        private int lastAdoptionTick = -CooldownTicks;

        public bool CanAdoptNow => Active && Find.TickManager.TicksGame >= lastAdoptionTick + CooldownTicks;

        public int CooldownTicksRemaining => CanAdoptNow ? 0 : lastAdoptionTick + CooldownTicks - Find.TickManager.TicksGame;

        public void Notify_AdoptionCompleted()
        {
            lastAdoptionTick = Find.TickManager.TicksGame;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastAdoptionTick, "lastAdoptionTick", -CooldownTicks);
        }
    }
}
