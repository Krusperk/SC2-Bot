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
            Controller.NukeAttack();

            #endregion

            #region Production/Construction
            Controller.ProductionDesire(Units.REFINERY, 2);
            Controller.ProductionDesire(Units.BARRACKS, 1);
            Controller.ProductionDesire(Units.GHOST_ACADEMY, 3);
            Controller.ProductionDesire(Units.BARRACKS_TECHLAB, 1);
            Controller.ProductionDesire(Units.FACTORY, 1);
            Controller.Research(Abilities.RESEARCH_GHOST_CLOAK);


            if (Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
            {
                foreach (var commCenter in Controller.GetUnits(Units.COMMAND_CENTER))
                {
                    Controller.ProductionDesire(Units.ORBITAL_COMMAND, produceFrom: commCenter);
                } 
            }

            foreach (var resCenter in Controller.GetUnits(Units.ResourceCenters))
            {
                if (Controller.GetTotalCount(Units.SCV) < resCenter.idealWorkers + Controller.gassCapacity * 2) // ToDo: resCenter.idealWorkers + refineries.Count * Controller.gassCapacity
                    Controller.ProductionDesire(Units.SCV, produceFrom: resCenter);
            }

            foreach (var barrack in Controller.GetUnits(Units.BARRACKS, onlyCompleted: true))
            {
                if (Controller.GetTotalCount(Units.GHOST) < 5)
                    Controller.ProductionDesire(Units.GHOST, produceFrom: barrack); 

                //if (Controller.GetTotalCount(Units.MARINE) < 5)
                //    Controller.ProductionDesire(Units.MARINE, produceFrom: barrack);
            }

            foreach (var ghostAca in Controller.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true))
            {
                Controller.ProductionDesire(Units.NUKE, produceFrom: ghostAca);
            }

            //ProductionAim(Units.BARRACKS, 3);
            //ProductionAim(Units.GHOST_ACADEMY, 3);
            #endregion

            return Controller.CloseFrame();
        }
    }
}
