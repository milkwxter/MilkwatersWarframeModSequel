using RimWorld;
using VanillaWeaponsExpandedLaser;
using Verse;
using UnityEngine;

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
            base.Impact(hitThing, blockedByShield);

            // Custom logic
            if (hitThing is Pawn pawn)
            {
                // Logic for critical hits
                if (Rand.Chance(def.critChance) && pawn != null)
                {
                    // Throw a mote
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Critical hit", Color.red, 3.65f);

                    // Deal damage
                    DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount * def.critDamage, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                    pawn.TakeDamage(dinfo);
                }
                // Logic for status effects
                if (Rand.Chance(def.hediffChance) && !pawn.Dead)
                {
                    HediffDef hediffDef = def.hediffToAdd;
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    Log.Message("Added hediff '" + def.hediffToAdd.defName + "' to pawn named " + pawn.Name);
                }
            }
        }
    }

    public class LaserBeamWarframe : LaserBeam
    {
        private new LaserBeamWarframeDef def => base.def as LaserBeamWarframeDef;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);

            // Custom logic
            if (hitThing is Pawn pawn)
            {
                // Logic for critical hits
                if (Rand.Chance(def.critChance) && pawn != null)
                {
                    // Throw a mote
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Critical hit", Color.red, 3.65f);

                    // Deal damage
                    DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount * def.critDamage, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                    pawn.TakeDamage(dinfo);
                }
                // Logic for status effects
                if (Rand.Chance(def.hediffChance) && !pawn.Dead)
                {
                    HediffDef hediffDef = def.hediffToAdd;
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    Log.Message("Added hediff '" + def.hediffToAdd.defName + "' to pawn named " + pawn.Name);
                }
            }
        }
    }
}
