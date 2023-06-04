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
    public static class MainController {
        //editable
        private static readonly int frameDelay = 0; //too fast? increase this to e.g. 20

        //don't edit
        private static readonly List<Action> actions = new List<Action>();
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
                var resourceCenters = UnitController.GetUnits(Units.ResourceCenters);
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
    }
}