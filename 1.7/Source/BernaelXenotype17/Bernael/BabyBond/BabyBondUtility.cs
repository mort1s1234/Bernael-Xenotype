using System.Collections.Generic;
using Verse;

namespace Bernael_Xenotype
{
    public static class BabyBondUtility
    {

        public static void EstablishBond(Pawn feeder, Pawn baby)
        {
            if (feeder == null || baby == null)
            {
                return;
            }
            if (feeder.Dead || baby.Dead)
            {
                return;
            }

            HediffComp_BabyBond_Feeder feederComp = GetFeederComp(feeder);
            if (feederComp == null)
            {
                Hediff feederHediff = HediffMaker.MakeHediff(BernaelDefOf.BX_BabyBond_Feeder, feeder);
                feeder.health.AddHediff(feederHediff);
                feederComp = feederHediff.TryGetComp<HediffComp_BabyBond_Feeder>();

                if (feederComp == null)
                {
                    return;
                }
            }
            if (feederComp.IsBonded(baby))
            {
                return;
            }
            feederComp.AddBaby(baby);

            HediffComp_BabyBond_Victim babyComp = GetBabyComp(baby);
            if (babyComp == null)
            {
                Hediff babyHediff = HediffMaker.MakeHediff(BernaelDefOf.BX_BabyBond_Victim, baby);
                baby.health.AddHediff(babyHediff);
                babyComp = babyHediff.TryGetComp<HediffComp_BabyBond_Victim>();

                if (babyComp == null)
                {
                    return;
                }
            }
            babyComp.BondedFeeder = feeder;
        }

        public static void BreakBond(Pawn feeder, Pawn baby)
        {
            if (feeder == null || baby == null) return;

            HediffComp_BabyBond_Feeder feederComp = GetFeederComp(feeder);
            feederComp?.RemoveBaby(baby);

            if (!baby.Dead)
            {
                HediffComp_BabyBond_Victim babyComp = GetBabyComp(baby);
                if (babyComp != null)
                {
                    baby.health.RemoveHediff(babyComp.parent);
                }
            }
        }


        public static void BreakAllBonds(Pawn feeder)
        {
            if (feeder == null) return;
            HediffComp_BabyBond_Feeder feederComp = GetFeederComp(feeder);
            if (feederComp == null) return;

            List<Pawn> babies = new List<Pawn>(feederComp.BondedBabies);
            for (int i = 0; i < babies.Count; i++)
            {
                BreakBond(feeder, babies[i]);
            }
        }


        public static HediffComp_BabyBond_Feeder GetFeederComp(Pawn feeder)
        {
            if (feeder?.health?.hediffSet == null) return null;

            HediffWithComps hediff = feeder.health.hediffSet.GetFirstHediffOfDef(BernaelDefOf.BX_BabyBond_Feeder) as HediffWithComps;
            return hediff?.TryGetComp<HediffComp_BabyBond_Feeder>();
        }

        public static HediffComp_BabyBond_Victim GetBabyComp(Pawn baby)
        {
            if (baby?.health?.hediffSet == null) return null;

            HediffWithComps hediff = baby.health.hediffSet.GetFirstHediffOfDef(BernaelDefOf.BX_BabyBond_Victim) as HediffWithComps;
            return hediff?.TryGetComp<HediffComp_BabyBond_Victim>();
        }
    }
}
