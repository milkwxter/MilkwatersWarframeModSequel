using AlienRace;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WarframeModSequel
{
    public class WarframeMeleeThingDef : ThingDef
    {
        public float critChance;
        public float critDamage;
        public float hediffChance;
        public HediffDef hediffToAdd;
    }

    public class DamageWorker_AddInjuryWarframe : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing hitThing)
        {
            // Get stats from xml definition
            ThingDef weapon = dinfo.Weapon;
            WarframeMeleeThingDef def = (WarframeMeleeThingDef)weapon;
            
            if(def != null)
            {
                Log.Message(def.critChance);
                
                // Critical chance logic
                if (Rand.Chance(def.critChance) && hitThing is Pawn)
                {
                    // Throw a mote
                    MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "CRIT!", Color.red, 3.65f);

                    // Do a sound
                    DefDatabase<SoundDef>.GetNamed("Milkwater_Crit").PlayOneShot(new TargetInfo(hitThing.Position, hitThing.Map, false));

                    // Modify the weapon damage multiplier
                    dinfo.SetAmount(dinfo.Amount * def.critDamage);
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
            }

            return base.Apply(dinfo, hitThing);
        }
    }
}
