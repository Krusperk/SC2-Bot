using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot
{
    internal abstract class BaseBot : IBot
    {
        //the following will be called every frame
        //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize  
        public virtual IEnumerable<Action> OnFrame()
        {
            Controller.OpenFrame();

            if (Controller.frame == 0)
            {
                Logger.Info("Bot");
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", Controller.gameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (Controller.frame == Controller.SecsToFrames(1))
                Controller.Chat("gl hf");

            var structures = Controller.GetUnits(Units.Structures);
            if (structures.Count == 1)
            {
                //last building                
                if (structures[0].integrity < 0.4) //being attacked or burning down                 
                    if (!Controller.chatLog.Contains("gg"))
                        Controller.Chat("gg");
            }


            //keep on buildings depots if supply is tight
            if (Controller.maxSupply - Controller.currentSupply <= 5)
                if (Controller.CanConstruct(Units.SUPPLY_DEPOT))
                    if (Controller.GetPendingCount(Units.SUPPLY_DEPOT) == 0)
                        Controller.Construct(Units.SUPPLY_DEPOT);


            //distribute workers optimally every 10 frames
            if (Controller.frame % 10 == 0)
                Controller.DistributeWorkers();


            //attack when we have enough units
            var army = Controller.GetUnits(Units.ArmyUnits);
            if (army.Count >= 5)
            {
                if (Controller.enemyLocations.Count > 0)
                    Controller.Attack(army, Controller.enemyLocations[0]);
            }

            return Controller.CloseFrame();
        }

        protected virtual void ProductionDesire(uint toProduce, uint count = uint.MaxValue, Unit produceFrom = null)
        {
            if (Controller.CanConstruct(toProduce)
                && Controller.GetTotalCount(toProduce) < count)
            {
                if (produceFrom == null)
                {
                    Controller.Construct(toProduce);
                }
                else
                {
                    produceFrom.Train(toProduce);
                }
            }
        }
    }
}
