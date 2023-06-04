using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot
{
    internal class KrushBot : BaseBot
    {
        public override IEnumerable<Action> OnFrame()
        {
            base.OnFrame();

            #region Unit actions

            // Ghost attempt to nuke
            CombatController.NukeAttack();

            #endregion

            #region Production/Construction
            ProductionController.ProductionDesire(Units.REFINERY, 2);
            ProductionController.ProductionDesire(Units.BARRACKS, 1);
            ProductionController.ProductionDesire(Units.GHOST_ACADEMY, 3);
            ProductionController.ProductionDesire(Units.BARRACKS_TECHLAB, 1);
            ProductionController.ProductionDesire(Units.FACTORY, 1);
            ResearchController.Research(Abilities.RESEARCH_GHOST_CLOAK);


            if (UnitController.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
            {
                foreach (var commCenter in UnitController.GetUnits(Units.COMMAND_CENTER))
                {
                    ProductionController.ProductionDesire(Units.ORBITAL_COMMAND, produceFrom: commCenter);
                } 
            }

            foreach (var resCenter in UnitController.GetUnits(Units.ResourceCenters))
            {
                if (resCenter.assignedWorkers < resCenter.idealWorkers)
                    ProductionController.ProductionDesire(Units.SCV, produceFrom: resCenter);
            }

            foreach (var barrack in UnitController.GetUnits(Units.BARRACKS, onlyCompleted: true))
            {
                if (UnitController.GetTotalCount(Units.GHOST) < 10)
                    ProductionController.ProductionDesire(Units.GHOST, produceFrom: barrack); 

                //if (Controller.GetTotalCount(Units.MARINE) < 5)
                //    Controller.ProductionDesire(Units.MARINE, produceFrom: barrack);
            }

            foreach (var ghostAca in UnitController.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true))
            {
                ProductionController.ProductionDesire(Units.NUKE, produceFrom: ghostAca);
            }

            //ProductionAim(Units.BARRACKS, 3);
            //ProductionAim(Units.GHOST_ACADEMY, 3);
            #endregion

            return MainController.CloseFrame();
        }
    }
}
