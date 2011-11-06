/*
 * Copyright (c) 2010 Jopirop
 * 
 * All rights reserved.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Cyclops {
    public delegate void CommandDelegate(object[] args);

    /// <summary>
    /// Handles game world commands for us.
    /// </summary>
    public class Command {
        //List of all the commands
        private static Dictionary<string, CommandInfo> commands =
        new Dictionary<string, CommandInfo>();

        /// <summary>
        /// Load the commands.
        /// </summary>
        public static void Load() {
            string path = Config.GetDataPath() + Config.GetCommandDirectory();
            FileInfo[] fileList = new DirectoryInfo(path).GetFiles();
            DynamicCompile dCompile = new DynamicCompile();
            foreach (FileInfo info in fileList) {
                CommandInfo commandInfo = new CommandInfo();
                dCompile.Compile(path + info.Name, commandInfo);
                commands.Add(commandInfo.Name.ToLower(), commandInfo);
            }
        }

        /// <summary>
        /// Execute the specified command.
        /// </summary>
        /// <param name="world">A reference to the game world.</param>
        /// <param name="gameMap">A reference to the game map.</param>
        /// <param name="msg">The creature's message.</param>
        public static void ExecuteCommand(GameWorld world, Map gameMap,
            string msg, Creature creature) {
            string[] parameters = Regex.Split(msg, " ");

            commands[parameters[0].ToLower()].CommandMethod.
                Invoke(new object[] { world, gameMap, parameters, creature });
        }

        /// <summary>
        /// Gets whether this message is a command. Note: Returns true
        /// only if it is a command and the creature has a high
        /// enough access level.
        /// </summary>
        /// <param name="msg">The message to test</param>
        /// <param name="creature">The creature for whom to test</param>
        /// <returns>True if it is a command and creature has valid access
        /// or false otherwise</returns>
        public static bool IsCommand(string msg, Creature creature) {
            string[] parameters = Regex.Split(msg, " ");
            if (parameters.Length == 0) {
                return false;
            }
            string command = parameters[0];
            if (!commands.ContainsKey(command.ToLower())) {
                return false;
            }
            return true; //TODO: Uncomment the bottom command
            //            return creature.Access >= commands[command.ToLower()].AccessLevel;
        }

        //TODO: Delete
        public Object Method(Object arg) {
            SpellInfo spellInfo = (SpellInfo)arg;
            spellInfo.Name = "exisa mas";
            spellInfo.Type = SpellType.PLAYER_SAY;
            spellInfo.InitDelegate = delegate(Object[] args) {
                string argument = (string)args[0];
                Player player = (Player)args[1];
                Spell spell = (Spell)args[2];

                spell.SpellArea = new bool[,] { { true } };
                spell.SpellCenter = player.CurrentPosition.Clone();
                spell.MaxDmg = 0;
                spell.MinDmg = 0;
                spell.RequiredMLevel = 4;
                spell.ManaCost = 90;
                spell.SpellEffect = MagicEffect.GREEN_SPARKLES;
                spell.VocationsFor = new Vocation[] { Vocation.SORCERER, Vocation.KNIGHT, Vocation.DRUID, Vocation.PALADIN};

                spell.Action = delegate(GameWorld world, Position hitPosition, List<Thing> hitBySpell) {
                    world.AppendBroadcast(player, argument);
                };
            };
            return null;
        }
    }

    /// <summary>
    /// Stores information about a command.
    /// </summary>
    public class CommandInfo {
        /// <summary>
        /// Gets and sets the command name.
        /// </summary>
        public string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the access level required
        /// to execute this command.
        /// </summary>
        public byte AccessLevel {
            get;
            set;
        }

        /// <summary>
        /// The command delegate to invoke in order to
        /// execute this command.
        /// </summary>
        public CommandDelegate CommandMethod {
            get;
            set;
        }
    }
}
