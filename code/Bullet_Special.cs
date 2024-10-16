using RimWorld;
using VanillaWeaponsExpandedLaser;
using Verse;
using UnityEngine;
using Verse.Sound;
using System.Collections.Generic;
using Verse.Noise;

namespace WarframeModSequel
{
    public class ProjectileWarframeDef : ThingDef
    {
        public HediffDef hediffToAdd;
        public float hediffChance;

        public float critChance;
        public float critDamage;
    }

    public class LaserBeamWarframeDef : LaserBeamDef
    {
        public HediffDef hediffToAdd;
        public float hediffChance;

        public float critChance;
        public float critDamage;
    }

    public class ProjectileWarframe : Bullet
    {
        private new ProjectileWarframeDef def => base.def as ProjectileWarframeDef;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // Critical damage logic
            if (Rand.Chance(def.critChance) && hitThing is Pawn)
            {
                // Throw a mote
                MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "CRIT!", Color.red, 3.65f);

                // Do a sound
                DefDatabase<SoundDef>.GetNamed("Milkwater_Crit").PlayOneShot(new TargetInfo(hitThing.Position, hitThing.Map, false));

                // Modify the weapon damage multiplier
                this.weaponDamageMultiplier *= def.critDamage;
            }
            // Logic for status effects
            if (Rand.Chance(def.hediffChance))
            {
                if (hitThing is Pawn pawn)
                {
                    HediffDef hediffDef = def.hediffToAdd;
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    Log.Message("Added hediff '" + def.hediffToAdd.defName + "' to pawn named " + pawn.Name);
                }
            }

            // Do the actual impact
            base.Impact(hitThing, blockedByShield);
        }
    }

    public class LaserBeamWarframe : LaserBeam
    {
        private new LaserBeamWarframeDef def => base.def as LaserBeamWarframeDef;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // Critical damage logic
            if (Rand.Chance(def.critChance) && hitThing is Pawn)
            {
                // Throw a mote
                MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "CRIT!", Color.red, 3.65f);

                // Do a sound
                DefDatabase<SoundDef>.GetNamed("Milkwater_Crit").PlayOneShot(new TargetInfo(hitThing.Position, hitThing.Map, false));

                // Modify the weapon damage multiplier
                this.weaponDamageMultiplier *= def.critDamage;
            }
            // Logic for status effects
            if (Rand.Chance(def.hediffChance))
            {
                if (hitThing is Pawn pawn)
                {
                    HediffDef hediffDef = def.hediffToAdd;
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    Log.Message("Added hediff '" + def.hediffToAdd.defName + "' to pawn named " + pawn.Name);
                }
            }

            // Do the actual impact
            base.Impact(hitThing, blockedByShield);
        }
    }
}