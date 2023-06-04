using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public static class ResearchController
    {
        public static void Research(int toResearch)
        {
            if (CanResearch(toResearch))
            {
                ulong unitTag = 0;

                if (toResearch == Abilities.RESEARCH_GHOST_CLOAK)
                    unitTag = UnitController.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).First().tag;

                var researchAction = MainController.CreateRawUnitCommand(toResearch, unitTag);

                MainController.AddAction(researchAction);
            }
        }

        private static bool CanResearch(int toResearch)
        {
            if (toResearch == Abilities.RESEARCH_GHOST_CLOAK
                && UnitController.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).Any())
            {
                return CanAffortResearch(toResearch);
            }

            return false;
        }

        private static bool CanAffortResearch(int toResearch)
        {
            if (toResearch == Abilities.RESEARCH_GHOST_CLOAK)
                return MainController.minerals > 150 && MainController.vespene > 150;

            return false;
        }
    }
}
