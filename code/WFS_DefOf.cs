using AlienRace;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WarframeModSequel
{
    public static class WFS_Globals
    {
        public static List<ThingDef_AlienRace> warframeRaces = new List<ThingDef_AlienRace>
        {
            DefDatabase<ThingDef_AlienRace>.GetNamed("WarframeExcaliburRace"),
            DefDatabase<ThingDef_AlienRace>.GetNamed("WarframeEmberRace")
        };
    }

    [DefOf]
    public static class WFS_DefOf
    {
        public static JobDef UseSomaticLink;
        public static JobDef UseWarframeGestator;
        public static JobDef FillWarframeGestator;
        public static EffecterDef OperatingSomaticLink;
        static WFS_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WFS_DefOf));
        }
    }
}
