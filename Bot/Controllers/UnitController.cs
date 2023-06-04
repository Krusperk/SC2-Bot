using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public static class UnitController
    {
        public static string GetUnitName(uint unitType)
        {
            return MainController.gameData.Units[(int)unitType].Name;
        }


        public static int GetTotalCount(uint unitType)
        {
            var constructionCount = GetUnits(unitType).Count;
            return constructionCount;
        }


        public static int GetPendingCount(uint unitType, bool inConstruction = true)
        {
            var workers = GetUnits(Units.Workers);
            var abilityID = Abilities.GetID(unitType);

            var counter = 0;

            //count workers that have been sent to build this structure
            foreach (var worker in workers)
            {
                if (worker.order.AbilityId == abilityID)
                    counter += 1;
            }

            //count buildings that are already in construction
            if (inConstruction)
            {
                foreach (var unit in GetUnits(unitType))
                    if (unit.buildProgress < 1)
                        counter += 1;
            }

            return counter;
        }


        public static List<Unit> GetUnits(HashSet<uint> hashset, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in MainController.obs.Observation.RawData.Units)
                if (hashset.Contains(unit.UnitType) && unit.Alliance == alliance)
                {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible))
                        continue;

                    units.Add(new Unit(unit));
                }
            return units;
        }


        public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in MainController.obs.Observation.RawData.Units)
                if (unit.UnitType == unitType && unit.Alliance == alliance)
                {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible))
                        continue;

                    units.Add(new Unit(unit));
                }
            return units;
        }


        public static void DistributeWorkers()
        {
            var workers = GetUnits(Units.Workers);
            var idleWorkers = workers.Where(w => w.order.AbilityId == 0);

            if (idleWorkers.FirstOrDefault() is Unit idleWorker)
            {
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                foreach (var rc in resourceCenters)
                {
                    //get one of the closer mineral fields
                    var mf = GetFirstInRange(rc.position, mineralFields, 7);
                    if (mf == null) continue;

                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", idleWorker.tag);
                    idleWorker.Smart(mf);
                    return;
                }
                //nothing to be done
                return;
            }
            else
            {
                //let's see if we can distribute between bases                
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                Unit transferFrom = null;
                Unit transferTo = null;
                foreach (var rc in resourceCenters)
                {
                    if (rc.assignedWorkers <= rc.idealWorkers)
                        transferTo = rc;
                    else
                        transferFrom = rc;
                }

                if ((transferFrom != null) && (transferTo != null))
                {
                    var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                    var sqrDistance = 7 * 7;
                    foreach (var worker in workers)
                    {
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

            // Alter worker count assigned to refinery
            foreach (var refinery in GetUnits(Units.REFINERY, Alliance.Self, true))
            {
                var miningPotenc = refinery.idealWorkers - refinery.assignedWorkers;
                if (miningPotenc > 0)
                {
                    foreach (var worker in GetAvailableWorkers(miningPotenc))
                    {
                        Logger.Info("Distributing worker to gas: {0}", worker.tag);
                        worker.Smart(refinery);
                    }
                }
                else if (miningPotenc < 0)
                {
                    var worker = GetFirstInRange(refinery.position, GetUnits(Units.Workers), 7);
                    var mineralField = GetFirstInRange(refinery.position, GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral), 7);
                    Logger.Info("Distributing gas worker to mineral: {0}", worker.tag);
                    worker.Smart(mineralField);
                }
            }
        }


        public static IEnumerable<Unit> GetAvailableWorkers(int count = 1) =>
            GetUnits(Units.Workers)
                .Where(w => w.order.AbilityId == Abilities.GATHER_MINERALS)
                .Take(count);


        public static Unit GetAvailableWorker() => GetAvailableWorkers(1).SingleOrDefault();


        public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }


        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;
            foreach (var unit in units)
            {
                if (Vector3.DistanceSquared(targetPosition, unit.position) <= maxDistanceSqr)
                    return unit;
            }
            return null;
        }
    }
}
