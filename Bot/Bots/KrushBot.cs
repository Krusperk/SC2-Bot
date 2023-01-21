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

            ProductionDesire(Units.REFINERY, 2);
            ProductionDesire(Units.BARRACKS, 1);
            ProductionDesire(Units.GHOST_ACADEMY, 1);
            ProductionDesire(Units.BARRACKS_TECHLAB, 1);
            ProductionDesire(Units.FACTORY, 1);


            if (Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
            {
                foreach (var commCenter in Controller.GetUnits(Units.COMMAND_CENTER))
                {
                    ProductionDesire(Units.ORBITAL_COMMAND, produceFrom: commCenter);
                } 
            }

            foreach (var resCenter in Controller.GetUnits(Units.ResourceCenters))
            {
                if (Controller.GetTotalCount(Units.SCV) < resCenter.idealWorkers + Controller.gassCapacity * 2) // ToDo: resCenter.idealWorkers + refineries.Count * Controller.gassCapacity
                    ProductionDesire(Units.SCV, produceFrom: resCenter);
            }

            foreach (var barrack in Controller.GetUnits(Units.BARRACKS, onlyCompleted: true))
            {
                ProductionDesire(Units.GHOST, produceFrom: barrack);

                if (Controller.GetTotalCount(Units.MARINE) < 5)
                    ProductionDesire(Units.MARINE, produceFrom: barrack);
            }

            foreach (var ghostAca in Controller.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true))
            {
                ProductionDesire(Units.NUKE, produceFrom: ghostAca);
            }

            //ProductionAim(Units.BARRACKS, 3);
            //ProductionAim(Units.GHOST_ACADEMY, 3);

            return Controller.CloseFrame();
        }
    }
}
