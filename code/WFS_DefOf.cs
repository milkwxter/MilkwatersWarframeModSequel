using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WarframeModSequel
{
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
