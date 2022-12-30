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

            ProductionDesire(Units.REFINERY, 1);
            ProductionDesire(Units.BARRACKS, 1);
            ProductionDesire(Units.REFINERY, 2);
            ProductionDesire(Units.GHOST_ACADEMY, 1);
            ProductionDesire(Units.BARRACKS_TECHLAB);
            ProductionDesire(Units.ORBITAL_COMMAND);
            ProductionDesire(Units.FACTORY, 1);

            foreach (var resCenter in Controller.GetUnits(Units.ResourceCenters))
            {
                if (Controller.GetTotalCount(Units.SCV) < resCenter.idealWorkers)
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
