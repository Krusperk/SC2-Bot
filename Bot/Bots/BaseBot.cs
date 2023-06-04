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
            MainController.OpenFrame();

            if (MainController.frame == 0)
            {
                Logger.Info("Bot");
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", MainController.gameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (MainController.frame == MainController.SecsToFrames(1))
                MainController.Chat("gl hf");

            var structures = UnitController.GetUnits(Units.Structures);
            if (structures.Count == 1)
            {
                //last building                
                if (structures[0].integrity < 0.4) //being attacked or burning down                 
                    if (!MainController.chatLog.Contains("gg"))
                        MainController.Chat("gg");
            }


            //keep on buildings depots if supply is tight
            if (MainController.maxSupply - MainController.currentSupply <= 5)
                if (ProductionController.CanConstruct(Units.SUPPLY_DEPOT))
                    if (UnitController.GetPendingCount(Units.SUPPLY_DEPOT) == 0)
                        ProductionController.Construct(Units.SUPPLY_DEPOT);


            //distribute workers optimally every 10 frames
            if (MainController.frame % 10 == 0)
                UnitController.DistributeWorkers();


            //attack when we have enough units
            var army = UnitController.GetUnits(Units.ArmyUnits);
            if (army.Count >= 20)
            {
                if (MainController.enemyLocations.Count > 0)
                    CombatController.Attack(army, MainController.enemyLocations[0]);
            }

            return MainController.CloseFrame();
        }
    }
}
