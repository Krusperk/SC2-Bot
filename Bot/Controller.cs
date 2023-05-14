using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;
// ReSharper disable MemberCanBePrivate.Global

namespace Bot {
    public static class Controller {
        //editable
        private static readonly int frameDelay = 0; //too fast? increase this to e.g. 20

        //don't edit
        private static readonly List<Action> actions = new List<Action>();
        private static readonly Random random = new Random();
        private const double FRAMES_PER_SECOND = 22.4;

        public static ResponseGameInfo gameInfo;
        public static ResponseData gameData;
        public static ResponseObservation obs;
        public static ulong frame;
        public static uint currentSupply;
        public static uint maxSupply;
        public static uint minerals;
        public static uint vespene;

        public static readonly List<Vector3> enemyLocations = new List<Vector3>();
        public static readonly List<string> chatLog = new List<string>();
        public static readonly uint gassCapacity = 3;

        public static void Pause() {
            Console.WriteLine("Press any key to continue...");
            while (Console.ReadKey().Key != ConsoleKey.Enter) {
                //do nothing
            }
        }

        public static ulong SecsToFrames(int seconds) {
            return (ulong) (FRAMES_PER_SECOND * seconds);
        }


        public static List<Action> CloseFrame() {
            return actions;
        }


        public static void OpenFrame() {
            if (gameInfo == null || gameData == null || obs == null) {
                if (gameInfo == null)
                    Logger.Info("GameInfo is null! The application will terminate.");
                else if (gameData == null)
                    Logger.Info("GameData is null! The application will terminate.");
                else
                    Logger.Info("ResponseObservation is null! The application will terminate.");
                Pause();
                Environment.Exit(0);
            }

            actions.Clear();

            foreach (var chat in obs.Chat) 
                chatLog.Add(chat.Message);

            frame = obs.Observation.GameLoop;
            currentSupply = obs.Observation.PlayerCommon.FoodUsed;
            maxSupply = obs.Observation.PlayerCommon.FoodCap;
            minerals = obs.Observation.PlayerCommon.Minerals;
            vespene = obs.Observation.PlayerCommon.Vespene;

            //initialization
            if (frame == 0) {
                var resourceCenters = GetUnits(Units.ResourceCenters);
                if (resourceCenters.Count > 0) {
                    var rcPosition = resourceCenters[0].position;

                    foreach (var startLocation in gameInfo.StartRaw.StartLocations) {
                        var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                        var distance = Vector3.Distance(enemyLocation, rcPosition);
                        if (distance > 30)
                            enemyLocations.Add(enemyLocation);
                    }
                }
            }

            if (frameDelay > 0)
                Thread.Sleep(frameDelay);
        }


        public static string GetUnitName(uint unitType) {
            return gameData.Units[(int) unitType].Name;
        }

        public static void AddAction(Action action) {
            actions.Add(action);
        }


        public static void Chat(string message, bool team = false) {
            var actionChat = new ActionChat();
            actionChat.Channel = team ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast;
            actionChat.Message = message;

            var action = new Action();
            action.ActionChat = actionChat;
            AddAction(action);
        }


        public static void Attack(List<Unit> units, Vector3 target) {
            var action = CreateRawUnitCommand(Abilities.ATTACK);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.tag);
            AddAction(action);
        }


        public static int GetTotalCount(uint unitType) {
            var constructionCount = GetUnits(unitType).Count;
            return constructionCount;
        }

        public static int GetPendingCount(uint unitType, bool inConstruction=true) {
            var workers = GetUnits(Units.Workers);
            var abilityID = Abilities.GetID(unitType);
            
            var counter = 0;
            
            //count workers that have been sent to build this structure
            foreach (var worker in workers) {
                if (worker.order.AbilityId == abilityID)
                    counter += 1;
            }

            //count buildings that are already in construction
            if (inConstruction) {  
                foreach (var unit in GetUnits(unitType))
                    if (unit.buildProgress < 1)
                        counter += 1;
            }

            return counter;
        }

        public static List<Unit> GetUnits(HashSet<uint> hashset, Alliance alliance=Alliance.Self, bool onlyCompleted=false, bool onlyVisible=false) {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in obs.Observation.RawData.Units)
                if (hashset.Contains(unit.UnitType) && unit.Alliance == alliance) {                    
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;
                    
                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible))
                        continue;
                                        
                    units.Add(new Unit(unit));
                }
            return units;
        }

        public static List<Unit> GetUnits(uint unitType, Alliance alliance=Alliance.Self, bool onlyCompleted=false, bool onlyVisible=false) {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in obs.Observation.RawData.Units)
                if (unit.UnitType == unitType && unit.Alliance == alliance) {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible))
                        continue;

                    units.Add(new Unit(unit));
                }
            return units;
        }


        public static bool CanAfford(uint unitType) {
            var unitData = gameData.Units[(int) unitType];

            // Orbital command upg is 400 cheeper than actual orbital command
            uint discount = unitType == Units.ORBITAL_COMMAND ? 400U : 0U;

            return (minerals >= unitData.MineralCost - discount) 
                && (vespene >= unitData.VespeneCost);
        }

        public static bool CanConstruct(uint unitType) {
            //is it a structure?
            if (Units.Structures.Contains(unitType)) {
                //we need worker for every structure
                if (GetUnits(Units.Workers).Count == 0) 
                    return false;

                //we need an RC for any structure
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted:true);
                if (resourceCenters.Count == 0) 
                    return false;
                
                if ((unitType == Units.COMMAND_CENTER) || (unitType == Units.SUPPLY_DEPOT))
                    return CanAfford(unitType);
                
                //we need supply depots for the following structures
                if (!GetUnits(Units.SupplyDepots, onlyCompleted: true).Any()) 
                    return false;

                if (unitType == Units.REFINERY)
                    return CanAfford(unitType);

                if (unitType == Units.BARRACKS)
                    return CanAfford(unitType);

                // We need barracks for the following structures
                if (GetUnits(Units.BARRACKS, onlyCompleted: true).Count == 0)
                    return false;

                if (unitType == Units.GHOST_ACADEMY)
                    return CanAfford(unitType);

                if (unitType == Units.BARRACKS_TECHLAB)
                    return GetUnits(Units.BARRACKS, onlyCompleted: true).Count > GetUnits(Units.BarracksAddOns, onlyCompleted: true).Count
                            && CanAfford(unitType);

                if (unitType == Units.ORBITAL_COMMAND)
                    return CanAfford(unitType);

                if (unitType == Units.FACTORY)
                    return CanAfford(unitType);

                // We need factory for the following "structures"
                if (GetUnits(Units.FACTORY, onlyCompleted: true).Count == 0)
                    return false;

            }
            //it's an actual unit
            else
            {                
                //do we have enough supply?
                var requiredSupply = Controller.gameData.Units[(int) unitType].FoodRequired;
                if (requiredSupply > (maxSupply - currentSupply))
                    return false;

                //do we construct the units from Barracks? 
                if (Units.FromBarracks.Contains(unitType)) {
                    if (!GetUnits(Units.BARRACKS, onlyCompleted: true).Any()) 
                        return false;
                }

                //do we construct the units from Ghost academy? 
                if (Units.FromGhostAcademy.Contains(unitType))
                {
                    if (!GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).Any())
                        return false;

                    // Nuke requires also Factory
                    if (unitType == Units.NUKE
                        && !GetUnits(Units.FACTORY, onlyCompleted: true).Any())
                    {
                        return false;
                    }       
                }
            }
            
            return CanAfford(unitType);
        }


        public static Action CreateRawUnitCommand(int ability = -1, ulong unitTag = 0) {
            var action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            if (ability != -1)
                action.ActionRaw.UnitCommand.AbilityId = ability;
            if (unitTag != 0)
                action.ActionRaw.UnitCommand.UnitTags.Add(unitTag);
            return action;
        }


        public static bool CanPlace(uint unitType, Vector3 targetPos) {
            //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
            var abilityID = Abilities.GetID(unitType);
            
            RequestQueryBuildingPlacement queryBuildingPlacement = new RequestQueryBuildingPlacement();
            queryBuildingPlacement.AbilityId = abilityID;
            queryBuildingPlacement.TargetPos = new Point2D();
            queryBuildingPlacement.TargetPos.X = targetPos.X;
            queryBuildingPlacement.TargetPos.Y = targetPos.Y;
            
            Request requestQuery = new Request();
            requestQuery.Query = new RequestQuery();
            requestQuery.Query.Placements.Add(queryBuildingPlacement);

            var result = Program.gc.SendQuery(requestQuery.Query);
            if (result.Result.Placements.Count > 0)
                return (result.Result.Placements[0].Result == ActionResult.Success);
            return false;
        }


        public static void DistributeWorkers() 
        {            
            var workers = GetUnits(Units.Workers);
            List<Unit> idleWorkers = new List<Unit>();
            foreach (var worker in workers) 
            {
                if (worker.order.AbilityId != 0) 
                    continue;
                idleWorkers.Add(worker);
            }
            
            if (idleWorkers.Count > 0) 
            {
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted:true);
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance:Alliance.Neutral);
                
                foreach (var rc in resourceCenters) {
                    //get one of the closer mineral fields
                    var mf = GetFirstInRange(rc.position, mineralFields, 7);
                    if (mf == null) continue;
                    
                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", idleWorkers[0].tag);                    
                    idleWorkers[0].Smart(mf);                                        
                    return;
                }
                //nothing to be done
                return;
            }
            else 
            {
                //let's see if we can distribute between bases                
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted:true);
                Unit transferFrom = null;
                Unit transferTo = null;
                foreach (var rc in resourceCenters) {
                    if (rc.assignedWorkers <= rc.idealWorkers)
                        transferTo = rc;
                    else
                        transferFrom = rc;
                }

                if ((transferFrom != null) && (transferTo != null)) {
                    var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance:Alliance.Neutral);
                    
                    var sqrDistance = 7 * 7;
                    foreach (var worker in workers) {
                        if (worker.order.AbilityId != Abilities.GATHER_MINERALS
                            || Vector3.DistanceSquared(worker.position, transferFrom.position) > sqrDistance
                            || !(GetFirstInRange(transferTo.position, mineralFields, 7) is Unit mf))
                        {
                            continue;
                        }
                    
                        //only one at a time
                        Logger.Info("Distributing idle worker: {0}", worker.tag);
                        worker.Smart(mf);                    
                        return;
                    }
                }
            }

            // Transfere mining workers to refinery
            foreach(var refinery in GetUnits(Units.REFINERY))
            {
                var miningPotenc = refinery.idealWorkers - refinery.assignedWorkers - 1;
                if (miningPotenc > 0)
                {
                    foreach (var worker in GetAvailableWorkers(miningPotenc))
                        worker.Smart(refinery);
                }
            }
        }

        public static IEnumerable<Unit> GetAvailableWorkers(int count = 1) =>
            GetUnits(Units.Workers)
                .Where(w => w.order.AbilityId == Abilities.GATHER_MINERALS)
                .Take(count);
        

        public static Unit GetAvailableWorker() => GetAvailableWorkers(1).Single();

        public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }
        
        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;
            foreach (var unit in units) {
                if (Vector3.DistanceSquared(targetPosition, unit.position) <= maxDistanceSqr)
                    return unit;
            }
            return null;
        }

        public static void Construct(uint unitType) {
            Vector3 startingSpot;

            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
                startingSpot = resourceCenters[0].position;
            else {                
                Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(unitType));
                return;
            }

            const int radius = 12;
                              
            //trying to find a valid construction spot
            Vector3 constructionSpot = default;
            ulong targetTag = default;

            // Find who will create construction (worker, building)
            Unit creator = null;
            if (new[] { Units.BARRACKS_TECHLAB, Units.BARRACKS_REACTOR }.Contains(unitType))
            {
                creator = GetUnits(Units.BARRACKS).First();
            }
            else if (unitType == Units.ORBITAL_COMMAND)
            {
                creator = GetUnits(Units.COMMAND_CENTER).First();
            }
            else if (unitType == Units.REFINERY)
            {
                var gasGeyser = GetUnits(Units.GasGeysers, onlyVisible: true, alliance: Alliance.Neutral)
                                    .First();
                targetTag = gasGeyser.tag;
            }
            else
            {
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible:true, alliance:Alliance.Neutral); 
                // Try find appropriate place for 100 tries
                for (int i = 0; i < 100; i++)
                {
                    var constructionSpotAux = new Vector3(startingSpot.X + random.Next(-radius, radius + 1), startingSpot.Y + random.Next(-radius, radius + 1), 0);

                    //avoid building in the mineral line
                    if (IsInRange(constructionSpotAux, mineralFields, 5))
                        continue;

                    //check if the building fits
                    if (!CanPlace(unitType, constructionSpotAux))
                        continue;

                    constructionSpot = constructionSpotAux;

                    //ok, we found a spot
                    break;
                }
                if (constructionSpot == default)
                {
                    Logger.Info($"Cannot find place for {GetUnitName(unitType)}");
                    return;
                }
            }

            if (creator == null)
                creator = GetAvailableWorker(); 

            if (creator == null) {
                Logger.Error("Unable to find worker to construct: {0}", GetUnitName(unitType));
                return;
            }
            
            var abilityID = Abilities.GetID(unitType);
            var constructAction = CreateRawUnitCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(creator.tag);
            if (targetTag != default)
            {
                constructAction.ActionRaw.UnitCommand.TargetUnitTag = targetTag;
            }
            if (constructionSpot != default)
            {
                constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D
                {
                    X = constructionSpot.X,
                    Y = constructionSpot.Y
                }; 
            }
            AddAction(constructAction);

            Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(unitType), constructionSpot.X, constructionSpot.Y);
        }


        public static void ProductionDesire(uint toProduce, uint count = uint.MaxValue, Unit produceFrom = null)
        {
            if (CanConstruct(toProduce)
                && GetTotalCount(toProduce) < count)
            {
                if (produceFrom == null)
                    Construct(toProduce);
                else
                    produceFrom.Train(toProduce);
            }
        }

        public static void Research(int toResearch)
        {
            if (CanResearch(toResearch))
            {
                ulong unitTag = 0;

                if (toResearch == Abilities.RESEARCH_GHOST_CLOAK)
                    unitTag = GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).First().tag;
                
                var researchAction = CreateRawUnitCommand(toResearch, unitTag);

                AddAction(researchAction);
            }
        }

        private static bool CanResearch(int toResearch)
        {
            if (toResearch == Abilities.RESEARCH_GHOST_CLOAK
                && GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).Any())
            {
                return CanAffortResearch(toResearch);
            }

            return false;
        }

        private static bool CanAffortResearch(int toResearch)
        {
            if (toResearch == Abilities.RESEARCH_GHOST_CLOAK)
                return minerals > 150 && vespene > 150;

            return false;
        }


        public static void GhostNukeAction()
        {
            if (GetUnits(Units.GHOST).FirstOrDefault() is Unit ghost)
            {
                var target = enemyLocations[0];

                var action = CreateRawUnitCommand(Abilities.NUKE_CALLDOWN);
                action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
                action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
                action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
                action.ActionRaw.UnitCommand.UnitTags.Add(ghost.tag);
                AddAction(action);
            }
        }
    }
}