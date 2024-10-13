using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using VFECore;

namespace WarframeModSequel
{
    [DefOf]
    public static class WFS_DefOf
    {
        public static JobDef UseSomaticLink;
        static WFS_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WFS_DefOf));
        }
    }

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

            AddFinishAction(delegate
            {
                somaticLink.setOperatorPawn(null);
            });

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = delegate
                {
                    somaticLink.setOperatorPawn(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Never,
            };
        }
    }
}