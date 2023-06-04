using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public static class CombatController
    {
        public static void Attack(List<Unit> units, Vector3 target)
        {
            var action = MainController.CreateRawUnitCommand(Abilities.ATTACK);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.tag);
            MainController.AddAction(action);
        }

        public static void NukeAttack()
        {
            if (UnitController.GetUnits(Units.GHOST).Where(g => g.energy >= 125
                                                || (g.cloakState != CloakState.NotCloaked
                                                    && g.energy > 80))
                                     .FirstOrDefault()
                                     is Unit ghost
                //&& GetUnits(Units.NUKE, onlyCompleted: true).Any()
                )
            {
                GhostCloak(ghost);
                if (ghost.cloakState != CloakState.NotCloaked)
                {
                    NukeCalldown(ghost);
                }
            }
        }

        private static void GhostCloak(Unit ghost)
        {
            var cloak = MainController.CreateRawUnitCommand(Abilities.CLOAK);
            cloak.ActionRaw.UnitCommand.UnitTags.Add(ghost.tag);
            MainController.AddAction(cloak);
        }

        private static void NukeCalldown(Unit ghost)
        {
            var target = MainController.enemyLocations[0];
            //gameData.Upgrades.First().
            var nukeCalldown = MainController.CreateRawUnitCommand(Abilities.NUKE_CALLDOWN);
            nukeCalldown.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            nukeCalldown.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
            nukeCalldown.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
            nukeCalldown.ActionRaw.UnitCommand.UnitTags.Add(ghost.tag);
            MainController.AddAction(nukeCalldown);

        }
    }
}
