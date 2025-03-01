﻿using AlienRace;
using RimWorld;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using VanillaPsycastsExpanded;
using Verse;
using Verse.AI;
using Verse.Sound;
using VFECore.Abilities;
using Color = UnityEngine.Color;

namespace WarframeModSequel
{
    [StaticConstructorOnStartup]
    public class WarframeGestator : Building
    {
        // Custom variables
        protected PawnKindDef pawnKindToCraft;
        protected float ticksToFinish = 1000;
        protected float currentTicks = 0;
        protected bool isOn = false;
        protected int numParts = 0;

        // Stuff from Building_MechGestator
        private static Material FormingCycleBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f));
        private static Material FormingCycleUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f));
        private Graphic cylinderGraphic;
        private Graphic topGraphic;

        // Ensure our data is saved and loaded with the game
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref numParts, "numWarframeGestatorParts", 0);
            Scribe_Values.Look(ref isOn, "isWarframeGestatorOn", false);
            Scribe_Values.Look(ref currentTicks, "currentWarframeGestatorProgress", 0);
            Scribe_Defs.Look(ref pawnKindToCraft, "currentWarframeGestatorPawnKind");
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            if( numParts >= 3)
            {
                yield return new FloatMenuOption("Start warframe gestator", delegate
                {
                    Job job = new Job(WFS_DefOf.UseWarframeGestator, this);
                    myPawn.jobs.TryTakeOrderedJob(job);
                });
            }
            else
            {
                yield return new FloatMenuOption("Fill warframe gestator", delegate
                {
                    Thing part = this.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("WarframeParts")).FirstOrDefault();
                    if (part == null)
                    {
                        Log.Error("No warframe parts. Please craft some.");
                        return;
                    }
                    Job job = new Job(WFS_DefOf.FillWarframeGestator, this, part);
                    myPawn.jobs.TryTakeOrderedJob(job);
                });
            }
        }

        public PawnKindDef getPawnKindToCraft()
        {
            return pawnKindToCraft;
        }

        public void setPawnKindToCraft(PawnKindDef pawnKind)
        {
            this.pawnKindToCraft = pawnKind;

            // check to see if we are creating a new pawn
            if (pawnKindToCraft != null)
            {
                SoundDefOf.MechGestatorCycle_Started.PlayOneShot(this);
            }
        }

        public bool getIsOn()
        {
            return isOn;
        }

        public void setIsOn(bool newOn)
        {
            this.isOn = newOn;
        }

        public int getParts()
        {
            return numParts;
        }

        public void AddParts(Thing warframeParts)
        {
            int num = Mathf.Min(warframeParts.stackCount, 3 - numParts);
            if (num > 0)
            {
                numParts += num;
                warframeParts.SplitOff(num).Destroy();
            }
        }

        // this function spawns the pawn that was selected
        public void CraftPawnKind()
        {
            // generate the pawn
            Pawn pawn = PawnGenerator.GeneratePawn(pawnKindToCraft, Faction.OfPlayer);

            // set his age to 18
            pawn.ageTracker.AgeBiologicalTicks = (long)(18 * 3600000L);
            pawn.ageTracker.AgeChronologicalTicks = (long)(18 * 3600000L);

            // give him major passion for shooting and melee
            pawn.skills.GetSkill(SkillDefOf.Shooting).passion = Passion.Major;
            pawn.skills.GetSkill(SkillDefOf.Melee).passion = Passion.Major;

            // check if Rebound (Continued) is installed
            if (ModsConfig.IsActive("Mlie.Rebound"))
            {
                // add the specific trait to the pawn
                TraitDef myTrait = DefDatabase<TraitDef>.GetNamed("ProjectileInversion_Trait");
                pawn.story.traits.GainTrait(new Trait(myTrait));
            }

            // max out their psycast trees
            PawnKindAbilityExtension_Psycasts modExtension = pawn.kindDef.GetModExtension<PawnKindAbilityExtension_Psycasts>();
            if (modExtension != null)
            {
                CompAbilities comp = pawn.GetComp<CompAbilities>();
                foreach (PathUnlockData unlockedPath in modExtension.unlockedPaths)
                {
                    unlockedPath.path.abilities.ForEach(ability =>
                    {
                        // unlock this ability
                        comp.GiveAbility(ability);
                    });
                }
            }

            // give him the psyfocus regeneration hediff
            HediffDef hediffDef = HediffDef.Named("WF_Generic_PsyfocusRegen");
            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);

            // give him the natural armor hediff
            hediffDef = HediffDef.Named("WF_Generic_NaturalArmor");
            hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);

            // spawn the pawn at the interaction cell
            GenSpawn.Spawn(pawn, this.InteractionCell, this.Map);

            // special effects
            FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WFS_Steam_Fleck"));
            DefDatabase<SoundDef>.GetNamed("Milkwater_WarframeDoneSteam").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));

            // tell the player the warframe is done
            Find.LetterStack.ReceiveLetter("Warframe completed", "Your new warframe is complete. Build a cryopod for them to rest in, and a somatic link to begin controlling them.", LetterDefOf.PositiveEvent);
        }

        public override void Tick()
        {
            if (isOn)
            {
                if(numParts >= 3)
                {
                    currentTicks += 1;
                    Log.Message("Current ticks: " + currentTicks + " | Total ticks: " + ticksToFinish);
                    if (currentTicks > ticksToFinish)
                    {
                        CraftPawnKind();

                        // reset all the variables
                        isOn = false;
                        pawnKindToCraft = null;
                        currentTicks = 0;
                        numParts -= 3;
                    }
                }
                else
                {
                    Log.Error("You need more warframe parts to start crafting");
                    isOn = false;
                    pawnKindToCraft = null;
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // do the basic draw stuff
            base.DrawAt(drawLoc, flip);

            // draw stuff if a warframe is being crafted
            if (pawnKindToCraft != null && isOn)
            {
                // draw the baby warframe
                Vector3 loc = drawLoc;
                loc.y += 1f / 52f;
                loc.z += 0.2f;
                loc.z += Mathf.PingPong((float)Find.TickManager.TicksGame * def.building.formingMechBobSpeed, def.building.formingMechYBobDistance);
                def.building.formingGraphicData.Graphic.Draw(loc, Rot4.North, this);

                // draw the progress bar
                Vector3 barLoc = drawLoc;
                barLoc.z -= 1f;
                GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest
                {
                    center = barLoc,
                    size = new Vector2(1f, 0.2f), // Size of the bar
                    fillPercent = currentTicks / ticksToFinish,
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f)),
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)),
                    margin = 0.1f
                };
                GenDraw.DrawFillableBar(r);
            }

            // draw the glass window and the top piece
            if (topGraphic == null)
            {
                topGraphic = def.building.mechGestatorTopGraphic.GraphicColoredFor(this);
            }
            if (cylinderGraphic == null)
            {
                cylinderGraphic = def.building.mechGestatorCylinderGraphic.GraphicColoredFor(this);
            }
            Vector3 loc2 = new Vector3(drawLoc.x, AltitudeLayer.BuildingBelowTop.AltitudeFor(), drawLoc.z);
            cylinderGraphic.Draw(loc2, base.Rotation, this);
            Vector3 loc3 = new Vector3(drawLoc.x, AltitudeLayer.BuildingOnTop.AltitudeFor(), drawLoc.z);
            topGraphic.Draw(loc3, base.Rotation, this);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Add custom variable to the inspect pane
            if(pawnKindToCraft == null)
            {
                stringBuilder.AppendLine("Currently crafting: Nothing");
            }
            else
            {
                stringBuilder.AppendLine("Currently crafting: " + pawnKindToCraft.label + " warframe");
            }
            stringBuilder.AppendLine("Warframe progress: " + currentTicks + "/" + ticksToFinish);
            stringBuilder.AppendLine("Current parts stored: " + numParts + "/3");

            return stringBuilder.ToString().TrimEnd();
        }
    }

    public class JobDriver_UseWarframeGestator : JobDriver
    {
        public WarframeGestator warframeGestator => job.GetTarget(TargetIndex.A).Thing as WarframeGestator;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (warframeGestator != null)
            {
                if (!pawn.Reserve(warframeGestator, job, 1, 0, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // make pawn go to building
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // make our toil
            Toil toil = ToilMaker.MakeToil("MakeNewToils");

            // initial action and setup
            toil.initAction = delegate
            {
                // ensure the pawn is at the interaction cell
                pawn.pather.StartPath(warframeGestator.InteractionCell, PathEndMode.OnCell);
                // open the dialog window
                Find.WindowStack.Add(new Dialog_StartCraftingWarframe(warframeGestator));
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            // final action if any
            toil.AddFinishAction(delegate
            {
                // nothing yet
            });

            // We are done
            yield return toil;
        }
    }

    public class JobDriver_FillWarframeGestator : JobDriver
    {
        public WarframeGestator warframeGestator => job.GetTarget(TargetIndex.A).Thing as WarframeGestator;

        protected Thing parts => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(warframeGestator, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(parts, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            job.count = 3 - warframeGestator.getParts();
            Toil reserveParts = Toils_Reserve.Reserve(TargetIndex.B);
            yield return reserveParts;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveParts, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                warframeGestator.AddParts(parts);
                DefDatabase<SoundDef>.GetNamed("Milkwater_LoadWarframeParts").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }
    }

    public class Dialog_StartCraftingWarframe : Window
    {
        private WarframeGestator building;
        private List<Pawn> pawns;

        public Dialog_StartCraftingWarframe(WarframeGestator building)
        {
            this.building = building;
            this.pawns = building.Map.mapPawns.AllHumanlikeSpawned;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 480f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "Choose a warframe to craft:");

            Text.Font = GameFont.Small;

            Rect scrollRect = new Rect(0, 40f, inRect.width, inRect.height - 40f - 50f);
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, pawns.Count * 30f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (PawnKindDef pawnKind in DefDatabase<PawnKindDef>.AllDefs.Where(pawnKind => WFS_Globals.warframeRaces.Contains(pawnKind.race)))
            {
                // Draw a portrait of the warframe
                Rect portraitRect = new Rect(0, y + 15, 30f, 30f);
                Widgets.DrawTextureFitted(portraitRect, getWarframeHeadTexture(pawnKind.race), 1.5f);

                // Draw the info of the warframe to craft
                Rect pawnRect = new Rect(40, y, viewRect.width - 130f, 60f);
                Widgets.Label(pawnRect, pawnKind.label.CapitalizeFirst() + ": " + pawnKind.race.description);

                // Draw a button
                if (Widgets.ButtonText(new Rect(viewRect.width - 100f, y, 100f, 60f), "Craft"))
                {
                    OnPawnKindDefSelected(pawnKind);
                    Close();
                }
                y += 60f;
            }

            Widgets.EndScrollView();
        }

        private Texture2D getWarframeHeadTexture(ThingDef warframeRace)
        {
            if (warframeRace == ThingDef_AlienRace.Named("WarframeEmberRace"))
                return ContentFinder<Texture2D>.Get("Races/Ember/Female_Average_Normal_east", true);
            else if (warframeRace == ThingDef_AlienRace.Named("WarframeExcaliburRace"))
                return ContentFinder<Texture2D>.Get("Races/Excalibur/Male_Average_Normal_east", true);
            else if (warframeRace == ThingDef_AlienRace.Named("WarframeRhinoRace"))
                return ContentFinder<Texture2D>.Get("Races/Rhino/Male_Average_Normal_east", true);
            else
                return ContentFinder<Texture2D>.Get("Races/Excalibur/Male_Average_Normal_south", true);
        }

        private void OnPawnKindDefSelected(PawnKindDef pawnKind)
        {
            if (building.getIsOn())
            {
                Log.Message("Already crafting: " + pawnKind.defName);
                return;
            }
            // Custom logic for when a pawn is selected
            Log.Message("Selected warframe to craft: " + pawnKind.defName);
            building.setPawnKindToCraft(pawnKind);
            building.setIsOn(true);
        }

        private Vector2 scrollPosition = Vector2.zero;
    }
}
