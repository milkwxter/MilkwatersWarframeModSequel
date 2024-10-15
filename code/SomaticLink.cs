﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WarframeModSequel
{
    public class SomaticLink : Building
    {
        protected Pawn operatorPawn;
        protected Pawn warframePawn;

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            yield return new FloatMenuOption("Operate somatic link", delegate
            {
                Job job = new Job(WFS_DefOf.UseSomaticLink, this);
                myPawn.jobs.TryTakeOrderedJob(job);
            });
        }

        public Pawn getOperatorPawn()
        {
            return operatorPawn;
        }

        public void setOperatorPawn(Pawn pawn)
        {
            operatorPawn = pawn;
        }

        public Pawn getWarframePawn()
        {
            return warframePawn;
        }

        public void setWarframePawn(Pawn pawn)
        {
            warframePawn = pawn;

            // do special effects haha
            if(warframePawn != null)
            {
                FleckMaker.Static(warframePawn.Position, warframePawn.Map, FleckDefOf.PsycastAreaEffect);
                DefDatabase<SoundDef>.GetNamed("Milkwater_OperateStart").PlayOneShot(new TargetInfo(warframePawn.Position, warframePawn.Map, false));
            }
        }
    }

    public class JobDriver_UseSomaticLink : JobDriver
    {
        public SomaticLink somaticLink => job.GetTarget(TargetIndex.A).Thing as SomaticLink;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (somaticLink != null)
            {
                if (!pawn.Reserve(somaticLink, job, 1, 0, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // make pawn go to somatic link
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // make our toil
            Toil toil = ToilMaker.MakeToil("MakeNewToils");

            // initial action and setup
            toil.initAction = delegate
            {
                // ensure the pawn is at the interaction cell
                pawn.pather.StartPath(somaticLink.InteractionCell, PathEndMode.OnCell);

                // tell the link who the operator is
                somaticLink.setOperatorPawn(pawn);

                // open the dialog window
                Find.WindowStack.Add(new Dialog_AssignWarframe(somaticLink));
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(WFS_DefOf.OperatingSomaticLink, TargetIndex.A);
            toil.PlaySustainerOrSound(SoundDefOf.MechGestator_Ambience);

            // final action
            toil.AddFinishAction(delegate
            {
                // do special effects haha
                Pawn operatorPawn = somaticLink.getOperatorPawn();
                if (operatorPawn != null && operatorPawn.Map != null && operatorPawn.Position.InBounds(operatorPawn.Map))
                {
                    FleckMaker.Static(operatorPawn.Position, operatorPawn.Map, FleckDefOf.PsycastAreaEffect);
                    DefDatabase<SoundDef>.GetNamed("Milkwater_OperateEnd").PlayOneShot(new TargetInfo(operatorPawn.Position, operatorPawn.Map, false));
                }

                // Remove links
                somaticLink.setOperatorPawn(null);
                somaticLink.setWarframePawn(null);
            });
            yield return toil;
        }
    }

    public class Dialog_AssignWarframe : Window
    {
        private SomaticLink building;
        private List<Pawn> pawns;

        public Dialog_AssignWarframe(SomaticLink building)
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
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "Choose a warframe to operate:");

            Text.Font = GameFont.Small;

            Rect scrollRect = new Rect(0, 40f, inRect.width, inRect.height - 40f - 50f);
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, pawns.Count * 30f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (Pawn pawn in pawns)
            {
                if (pawn.def.defName == "WarframeBaseRace")
                {
                    // Draw a portrait of the pawn
                    Rect portraitRect = new Rect(0, y, 30f, 30f);
                    Widgets.DrawTextureFitted(portraitRect, PortraitsCache.Get(pawn, new Vector2(30f, 30f), Rot4.South), 1.0f);

                    // Draw the name of the pawn beside it
                    Rect pawnRect = new Rect(30, y, viewRect.width - 30f, 30f);
                    Widgets.Label(pawnRect, pawn.Name.ToStringFull + ", " + pawn.genes.Xenotype.label);

                    // Draw a button
                    if (Widgets.ButtonText(new Rect(viewRect.width - 100f, y, 100f, 30f), "Operate"))
                    {
                        OnPawnSelected(pawn);
                        Close();
                    }
                    y += 30f;
                }
            }

            Widgets.EndScrollView();
        }

        private void OnPawnSelected(Pawn pawn)
        {
            // Custom logic for when a pawn is selected
            Log.Message("Selected warframe to operate: " + pawn.Name.ToStringShort);
            building.setWarframePawn(pawn);
        }

        private Vector2 scrollPosition = Vector2.zero;
    }
}