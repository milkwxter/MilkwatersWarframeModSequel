using RimWorld;
using Verse;
using Verse.AI;
using VFECore.Abilities;

namespace WarframeModSequel
{
    public class WarframePawn : Pawn
    {
        public bool linkBroken = true;

        public bool GetLinkStatus()
        {
            return linkBroken;
        }

        public override void Tick()
        {
            // do normal ticking stuff
            base.Tick();

            // Goated null checker
            if (this.Map == null || this.health == null && this.stances == null)
            {
                return;
            }

            // check if pawn is occupying a somatic link
            if (IsPawnInSomaticLink())
                linkBroken = false;
            else
                linkBroken = true;

            // check if somatic link connection is broken
            if (linkBroken)
            {
                this.health.AddHediff(DefDatabase<HediffDef>.GetNamed("WF_Generic_SomaticLinkBroken"));
            }
            else
            {
                Hediff hediff = this.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("WF_Generic_SomaticLinkBroken"));
                if (hediff != null)
                {
                    // Remove the hediff
                    this.health.RemoveHediff(hediff);
                }
            }
        }

        private bool IsPawnInSomaticLink()
        {
            // Check all maps
            foreach (Map map in Find.Maps)
            {
                // Check the currently checked map for a Somatic link
                foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDef.Named("WF_Somatic_Link")))
                {
                    // Check to make sure the thing is actually a somatic link, and make sure it has an operator
                    if (thing is SomaticLink somaticLink && somaticLink.getOperatorPawn() != null)
                    {
                        // Check if the controlled warframe is actually this pawn
                        if (somaticLink.getWarframePawn() == this)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
