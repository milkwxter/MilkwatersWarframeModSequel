using Verse;

namespace WarframeModSequel
{
    public class WarframePsyfocusRegenerationHediff : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();

            if (pawn.IsHashIntervalTick(3600))
            {
                // make sure the pawn has a psylink
                if (pawn.HasPsylink && pawn != null)
                {
                    pawn.psychicEntropy.OffsetPsyfocusDirectly(.01f);
                }
            }
        }
    }
}
