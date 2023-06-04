using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public static class ProductionController
    {
        private static readonly Random random = new Random();

        public static bool CanAffordUnit(uint unitType)
        {
            var unitData = MainController.gameData.Units[(int)unitType];

            // Orbital command upg is 400 cheeper than actual orbital command
            uint discount = unitType == Units.ORBITAL_COMMAND ? 400U : 0U;

            return (MainController.minerals >= unitData.MineralCost - discount)
                && (MainController.vespene >= unitData.VespeneCost);
        }

        public static bool CanConstruct(uint unitType)
        {
            //is it a structure?
            if (Units.Structures.Contains(unitType))
            {
                //we need worker for every structure
                if (UnitController.GetUnits(Units.Workers).Count == 0)
                    return false;

                //we need an RC for any structure
                var resourceCenters = UnitController.GetUnits(Units.ResourceCenters, onlyCompleted: true);
                if (resourceCenters.Count == 0)
                    return false;

                if (new[] { Units.COMMAND_CENTER, Units.SUPPLY_DEPOT, Units.REFINERY }.Contains(unitType))
                    return CanAffordUnit(unitType);

                //we need supply depots for the following structures
                if (!UnitController.GetUnits(Units.SupplyDepots, onlyCompleted: true).Any())
                    return false;

                if (unitType == Units.BARRACKS)
                    return CanAffordUnit(unitType);

                // We need barracks for the following structures
                if (UnitController.GetUnits(Units.BARRACKS, onlyCompleted: true).Count == 0)
                    return false;

                if (unitType == Units.GHOST_ACADEMY)
                    return CanAffordUnit(unitType);

                if (unitType == Units.BARRACKS_TECHLAB)
                    return UnitController.GetUnits(Units.BARRACKS, onlyCompleted: true).Count > UnitController.GetUnits(Units.BarracksAddOns, onlyCompleted: true).Count
                            && CanAffordUnit(unitType);

                if (unitType == Units.ORBITAL_COMMAND)
                    return CanAffordUnit(unitType);

                if (unitType == Units.FACTORY)
                    return CanAffordUnit(unitType);

                // We need factory for the following "structures"
                if (UnitController.GetUnits(Units.FACTORY, onlyCompleted: true).Count == 0)
                    return false;

            }
            //it's an actual unit
            else
            {
                //do we have enough supply?
                var requiredSupply = MainController.gameData.Units[(int)unitType].FoodRequired;
                if (requiredSupply > (MainController.maxSupply - MainController.currentSupply))
                    return false;

                //do we construct the units from Barracks? 
                if (Units.FromBarracks.Contains(unitType))
                {
                    if (!UnitController.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
                        return false;
                }

                //do we construct the units from Ghost academy? 
                if (Units.FromGhostAcademy.Contains(unitType))
                {
                    if (!UnitController.GetUnits(Units.GHOST_ACADEMY, onlyCompleted: true).Any())
                        return false;

                    // Nuke requires also Factory
                    if (unitType == Units.NUKE
                        && !UnitController.GetUnits(Units.FACTORY, onlyCompleted: true).Any())
                    {
                        return false;
                    }
                }
            }

            return CanAffordUnit(unitType);
        }

        public static bool CanPlace(uint unitType, Vector3 targetPos)
        {
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

        public static void Construct(uint unitType)
        {
            Vector3 startingSpot;

            var resourceCenters = UnitController.GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
                startingSpot = resourceCenters[0].position;
            else
            {
                Logger.Error("Unable to construct: {0}. No resource center was found.", UnitController.GetUnitName(unitType));
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
                creator = UnitController.GetUnits(Units.BARRACKS).First();
            }
            else if (unitType == Units.ORBITAL_COMMAND)
            {
                creator = UnitController.GetUnits(Units.COMMAND_CENTER).First();
            }
            else if (unitType == Units.REFINERY)
            {
                var gasGeyser = UnitController.GetUnits(Units.GasGeysers, onlyVisible: true, alliance: Alliance.Neutral)
                                    .First();
                targetTag = gasGeyser.tag;
            }
            else
            {
                var mineralFields = UnitController.GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
                // Try find appropriate place for 100 tries
                for (int i = 0; i < 100; i++)
                {
                    var constructionSpotAux = new Vector3(startingSpot.X + random.Next(-radius, radius + 1), startingSpot.Y + random.Next(-radius, radius + 1), 0);

                    //avoid building in the mineral line
                    if (UnitController.IsInRange(constructionSpotAux, mineralFields, 5))
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
                    Logger.Info($"Cannot find place for {UnitController.GetUnitName(unitType)}");
                    return;
                }
            }

            if (creator == null)
                creator = UnitController.GetAvailableWorker();

            if (creator == null)
            {
                Logger.Error("Unable to find worker to construct: {0}", UnitController.GetUnitName(unitType));
                return;
            }

            var abilityID = Abilities.GetID(unitType);
            var constructAction = MainController.CreateRawUnitCommand(abilityID);
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
            MainController.AddAction(constructAction);

            Logger.Info("Constructing: {0} @ {1} / {2}", UnitController.GetUnitName(unitType), constructionSpot.X, constructionSpot.Y);
        }

        public static void ProductionDesire(uint toProduce, uint count = uint.MaxValue, Unit produceFrom = null)
        {
            if (CanConstruct(toProduce)
                && UnitController.GetTotalCount(toProduce) < count)
            {
                if (produceFrom == null)
                    Construct(toProduce);
                else
                    produceFrom.Train(toProduce);
            }
        }
    }
}
