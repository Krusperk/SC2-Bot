using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot {
    internal class RaxBot : IBot {
        
        //the following will be called every frame
        //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize  
        public IEnumerable<Action> OnFrame() {
            MainController.OpenFrame();

            if (MainController.frame == 0) {
                Logger.Info("RaxBot");
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", MainController.gameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (MainController.frame == MainController.SecsToFrames(1)) 
                MainController.Chat("gl hf");

            var structures = UnitController.GetUnits(Units.Structures);
            if (structures.Count == 1) {
                //last building                
                if (structures[0].integrity < 0.4) //being attacked or burning down                 
                    if (!MainController.chatLog.Contains("gg"))
                        MainController.Chat("gg");                
            }

            var resourceCenters = UnitController.GetUnits(Units.ResourceCenters);
            foreach (var rc in resourceCenters) {
                if (ProductionController.CanConstruct(Units.SCV))
                    rc.Train(Units.SCV);
            }
            
            
            //keep on buildings depots if supply is tight
            if (MainController.maxSupply - MainController.currentSupply <= 5)
                if (ProductionController.CanConstruct(Units.SUPPLY_DEPOT))
                    if (UnitController.GetPendingCount(Units.SUPPLY_DEPOT) == 0)                    
                        ProductionController.Construct(Units.SUPPLY_DEPOT);

            
            //distribute workers optimally every 10 frames
            if (MainController.frame % 10 == 0)
                UnitController.DistributeWorkers();
            
            

            //build up to 4 barracks at once
            if (ProductionController.CanConstruct(Units.BARRACKS)) 
                if (UnitController.GetTotalCount(Units.BARRACKS) < 4)                
                    ProductionController.Construct(Units.BARRACKS);          
            
            //train marine
            foreach (var barracks in UnitController.GetUnits(Units.BARRACKS, onlyCompleted:true)) {
                if (ProductionController.CanConstruct(Units.MARINE))
                    barracks.Train(Units.MARINE);
            }

            //attack when we have enough units
            var army = UnitController.GetUnits(Units.ArmyUnits);
            if (army.Count > 20) {
                if (MainController.enemyLocations.Count > 0)
                    CombatController.Attack(army, MainController.enemyLocations[0]);
            }            

            return MainController.CloseFrame();
        }
    }
}