using AlienRace;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

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

        // Stuff from Building_MechGestator
        private static Material FormingCycleBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f));
        private static Material FormingCycleUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f));
        private Graphic cylinderGraphic;
        private Graphic topGraphic;

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            yield return new FloatMenuOption("Start warframe gestator", delegate
            {
                Job job = new Job(WFS_DefOf.UseWarframeGestator, this);
                myPawn.jobs.TryTakeOrderedJob(job);
            });
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

        public void craftPawnKind()
        {
            // Generate the pawn with the player's faction
            Pawn pawn = PawnGenerator.GeneratePawn(pawnKindToCraft, Faction.OfPlayer);

            // Spawn the pawn at the interaction cell
            GenSpawn.Spawn(pawn, this.InteractionCell, this.Map);

            // reset everything
            isOn = false;
            currentTicks = 0;

            // special effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.Smoke);
            DefDatabase<SoundDef>.GetNamed("Milkwater_WarframeDoneSteam").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
        }

        public override void Tick()
        {
            if (isOn)
            {
                currentTicks += 1;
                Log.Message("Current ticks: " + currentTicks + " | Total ticks: " + ticksToFinish);
                if (currentTicks > ticksToFinish)
                {
                    craftPawnKind();
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
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.yellow),
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 255f))
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

            // final action
            toil.AddFinishAction(delegate
            {
            });
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
            foreach (PawnKindDef pawnKind in DefDatabase<PawnKindDef>.AllDefs.Where(pawnKind => pawnKind.race == ThingDef_AlienRace.Named("WarframeBaseRace")))
            {
                // Draw a portrait of the warframe
                Rect portraitRect = new Rect(0, y, 30f, 30f);
                Widgets.DrawTextureFitted(portraitRect, ContentFinder<Texture2D>.Get("Races/Excalibur/Heads/Male_Average_Normal_east", true), 1.0f);

                // Draw the name of the warframe to craft
                Rect pawnRect = new Rect(30, y, viewRect.width - 30f, 30f);
                Widgets.Label(pawnRect, pawnKind.defName);

                // Draw a button
                if (Widgets.ButtonText(new Rect(viewRect.width - 100f, y, 100f, 30f), "Craft"))
                {
                    OnPawnKindDefSelected(pawnKind);
                    Close();
                }
                y += 30f;
            }

            Widgets.EndScrollView();
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
