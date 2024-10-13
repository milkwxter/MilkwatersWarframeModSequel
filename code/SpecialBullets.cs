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
            // Get basic variables
            Map map = base.Map;
            IntVec3 position = base.Position;

            // Do basic impact logic
            GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
            if (!blockedByShield && def.projectile.landedEffecter != null)
            {
                def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
            }
            Destroy();

            // Create a battle log entry
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);

            // Notify things around the projectile to be afraid
            NotifyImpact(hitThing, map, position);

            // Check to see if something was hit
            if (hitThing != null)
            {
                // See if the guy who shot the projectile needs to be guilty
                bool instigatorGuilty = !(launcher is Pawn launcherPawn) || !launcherPawn.Drafted;

                // Create the damage info
                DamageInfo dinfo;

                // Critical damage logic
                if (Rand.Chance(def.critChance) && hitThing is Pawn)
                {
                    // Throw a mote
                    MoteMaker.ThrowText(hitThing.DrawPos, map, "CRIT!", Color.red, 3.65f);

                    // Do a sound
                    DefDatabase<SoundDef>.GetNamed("Milkwater_Crit").PlayOneShot(new TargetInfo(hitThing.Position, map, false));

                    // Populate the damage info
                    dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount * def.critDamage, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                }
                // Normal damage logic
                else
                {
                    dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
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

                // Make the pawn take damage
                dinfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);

                // Tell the hit pawn to stagger
                Pawn pawn2 = hitThing as Pawn;
                pawn2?.stances?.stagger.Notify_BulletImpact(this);

                // All done!
                return;
            }
            // Check to see if the shot that missed hit a shield instead
            if (!blockedByShield)
            {
                // If not, do some cool flecks based on the environment
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map));
                if (base.Position.GetTerrain(map).takeSplashes)
                {
                    FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
                }
                else
                {
                    FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
                }
            }
        }
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData bulletImpactData = default(BulletImpactData);
            bulletImpactData.bullet = this;
            bulletImpactData.hitThing = hitThing;
            bulletImpactData.impactPosition = position;
            BulletImpactData impactData = bulletImpactData;
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
        }
    }

    public class LaserBeamWarframe : LaserBeam
    {
        private new LaserBeamWarframeDef def => base.def as LaserBeamWarframeDef;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // See if the guy who shot the projectile needs to be guilty
            bool instigatorGuilty = !(launcher is Pawn launcherPawn) || !launcherPawn.Drafted;

            // Create the damage info
            DamageInfo dinfo;

            // Critical damage logic
            if (Rand.Chance(def.critChance) && hitThing is Pawn)
            {
                // Throw a mote
                MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "CRIT!", Color.red, 3.65f);

                // Do a sound
                DefDatabase<SoundDef>.GetNamed("Milkwater_Crit").PlayOneShot(new TargetInfo(hitThing.Position, hitThing.Map, false));

                // Populate the damage info
                dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount * def.critDamage, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
            }
            // Normal damage logic
            else
            {
                dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
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

            // Make the pawn take damage
            dinfo.SetWeaponQuality(equipmentQuality);

            // Do the other thing
            base.Impact(hitThing, blockedByShield);
        }
    }
}
