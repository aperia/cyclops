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
    public delegate void InitSpell(Object[] args);
    public delegate bool SpellValidDel(GameWorld world, string msg);

    public delegate void SpellAction(GameWorld world, Position hitPosition, List<Thing> hitBySpell);

    /* This is the spell class, designed to be used for spells */
    public class Spell {

        private static Dictionary<string, SpellInfo> instantSpells =
            new Dictionary<string, SpellInfo>();

        private static Dictionary<string, SpellInfo> otherSpells =
            new Dictionary<string, SpellInfo>();

        /// <summary>
        /// Returns the spell name of a given string or null
        /// if it isn't a spell.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static string GetSpellName(string msg) {
            msg = msg.ToLower();
            string localMsg = msg.Replace('\"', ' ');
            string[] parameters = Regex.Split(localMsg, @"\s+");
            if (parameters.Length == 0) {
                return null;
            }
            string testingName = "";
            string spellName = "";
            bool isSpell = false;
            for (int i = 0; i < parameters.Length; i++) {
                if (testingName != "") {
                    testingName = testingName + " " + parameters[i];
                } else {
                    testingName = parameters[i];
                }
                if (instantSpells.ContainsKey(testingName)) {
                    isSpell = true;
                    spellName = testingName;
                }
            }

            if (isSpell) {
                return spellName;
            }

            return null;
        }

        private static void Compile(string path, Dictionary<string, SpellInfo> dict) {
            FileInfo[] fileList = new DirectoryInfo(path).GetFiles();
            DynamicCompile dCompile = new DynamicCompile();
            foreach (FileInfo info in fileList) {
                SpellInfo spellInfo = new SpellInfo();
                dCompile.Compile(path + info.Name, spellInfo);
                dict.Add(spellInfo.Name.ToLower(), spellInfo);
            }

        }
        public Spell() {
            //Number of vocations 
            int vocationsTotal = Enum.GetValues(typeof(Vocation)).Length;

            Name = "None";
            RequiredMLevel = 0;
            Immunity = ImmunityType.IMMUNE_PHYSICAL;
            DistanceEffect = DistanceType.EFFECT_NONE;
            SpellEffect = MagicEffect.YELLOW_RINGS;
            SpellCenter = new Position(0, 0, 0);
            MinDmg = 0;
            MaxDmg = 0;
            Rand = new Random(DateTime.Now.Millisecond);
            Rune = null;
            RequiresTarget = false;
        }

        /* /// <summary>
         /// This method returns a set of things that are in the
         /// spell's vicinity where the spell's vicinity is defined as
         /// the set of things that are either hit by the spell or for
         /// whom the spell effect is visible.
         /// </summary>
         /// <param name="map">A reference to the game map.</param>
         /// <returns>The things in this spell's vacinity.</returns>
         public ThingSet GetThingsInVicinity(Map map) {
             //TODO: Finish
             return map.GetThingsInVicinity(new Position(32097, 32217, 7));
         }*/


        public void CastSpell(Map map) {

        }

        public uint RequiredMLevel {
            get;
            set;
        }

        /// <summary>
        /// Perform any custom spell actions.
        /// </summary>
        public SpellAction Action {
            get;
            set;
        }

        public Vocation[] VocationsFor {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public ImmunityType Immunity {
            get;
            set;
        }

        public DistanceType DistanceEffect {
            get;
            set;
        }

        public Random Rand {
            get;
            set;
        }

        public bool[,] SpellArea {
            get;
            set;
        }

        public bool HasDistanceType() {
            return DistanceEffect != DistanceType.EFFECT_NONE;
        }

        public MagicEffect SpellEffect {
            get;
            set;
        }

        public uint ManaCost {
            get;
            set;
        }

        public Position SpellCenter {
            get;
            set;
        }

        public bool RequiresTarget {
            get;
            set;
        }

        public int MinDmg {
            get;
            set;
        }

        public int MaxDmg {
            get;
            set;
        }

        /// <summary>
        /// Get damage.
        /// </summary>
        /// <returns></returns>
        public int GetDamage() {
            return Rand.Next(MinDmg, MaxDmg);
        }

        /// <summary>
        /// Loads the spells.
        /// </summary>
        public static void Load() {
            string path = Config.GetDataPath() + Config.GetSpellDirectory();
            Compile(path + "instant" + System.IO.Path.DirectorySeparatorChar, instantSpells);
            Compile(path + "rune" + System.IO.Path.DirectorySeparatorChar, otherSpells);
            Compile(path + "monster" + System.IO.Path.DirectorySeparatorChar, otherSpells);
        }

        public static Spell CreateCreatureSpell(string name, string argument,
            Creature creature, int min, int max, Position spellCenter) {
            Spell spell = new Spell();
            otherSpells[name.ToLower()].InitDelegate.Invoke
                (new object[] { name, creature, min, max, spellCenter, spell, argument });
            return spell;
        }

        public static Spell CreateRuneSpell(string name, Player player, Position pos) {
            Spell spell = new Spell();
            otherSpells[name.ToLower()].InitDelegate.Invoke
                (new object[] { name, player, pos, spell });
            return spell;
        }

        public SpellValidDel IsSpellValid {
            get;
            set;
        }

        public Position UseWithPos {
            get;
            set;
        }

        public byte UseWithStackpos {
            get;
            set;
        }

        public Item Rune {
            get;
            set;
        }

        /// <summary>
        /// Create a spell that a player casts by speaking.
        /// </summary>
        /// <param name="name">The name of the spell.</param>
        /// <param name="player">The player casting.</param>
        /// <returns></returns>
        public static Spell CreateSpell(string name, Player player) {
            string arg = GetArgument(name);
            Spell spell = new Spell();
            name = GetSpellName(name);
            instantSpells[name.ToLower()].InitDelegate.
                Invoke(new object[] { arg, player, spell });
            return spell;
        }


        /// <summary>
        /// Gets whether a particular string is a spell name;
        /// </summary>
        /// <param name="spellName">The spell name to test.</param>
        /// <returns>True if it is a spell, false otherwise.</returns>
        public static bool IsSpell(string spellName, SpellType type) {
            string name = GetSpellName(spellName);
            if (name == null) {
                return false;
            }
            return (instantSpells[name].Type == type);
        }

        /// <summary>
        /// Gets the argument from a given string. An argument
        /// is defined as any characters after a spell name. For example,
        /// in utevo res "Dragon", Dragon is the argument. Returns the argument
        /// if this spell has an argument or an empty string if this
        /// spell doesn't have an argument.
        /// </summary>
        /// <returns>The argument if applicable or null if it isn't a spell.</returns>
        public static string GetArgument(string msg) {
            msg = msg.ToLower();
            string spellName = GetSpellName(msg);
            string localMsg = msg.Replace(spellName, "");
            localMsg = localMsg.Replace("\"", "");

            string[] parameters = Regex.Split(localMsg, "\\s+");
            if (parameters.Length == 0) {
                return "";
            }

            string argument = "";
            for (int i = 0; i < parameters.Length; i++) {
                if (argument != "") {
                    argument = argument + " " + parameters[i];
                } else {
                    argument = parameters[i];
                }
            }
            return argument;
        }

        //(//new object[] { name, creature, min, max, spellCenter, spell });

        /// <summary>
        /// Temporary method used for creating spells.
        /// TODO: Delete later.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Object blah(Object arg) {
            Dictionary<ushort, UseItemDelegate> del =
                   (Dictionary<ushort, UseItemDelegate>)arg;

            ushort[] foodIDs = {642, 1922, 2178, 3458, 387, 643, 899, 1411
                               , 1667, 2435, 2691, 1668, 2180, 133, 134, 136};
            ushort[] foodAmts = {180, 144, 120, 360, 72, 156, 96, 216, 12, 108,
                                      350, 60, 108, 24, 120, 108 };

            for (int i = 0; i < foodIDs.Length; i++) {
                ushort foodAmt = foodAmts[i];
                ushort ID = foodIDs[i];
                del.Add(ID, delegate(Item item, Player user, GameWorld world) {
                    user.AppendEatFood(foodAmt);
                    item.Count--;
                    if (item.Count == 0) {
                        world.AppendRemoveItem(item);
                    } else {
                        world.AppendUpdateItem(item);
                    }
                });
            }

            return null;

        }
    }

    public class SpellInfo {
        public string Name {
            get;
            set;
        }

        public InitSpell InitDelegate {
            get;
            set;
        }

        public SpellType Type {
            get;
            set;
        }

        //TODO: Delete all these blah methods

        public Object blahblah(Object arg) {
            Monster m = new Monster();
            return m;
        }

        public Object moreblah(Object arg) {

            Dictionary<ushort, UseItemDelegate> del =
                (Dictionary<ushort, UseItemDelegate>)arg;

            ushort[] foodIDs = {642, 1922, 2178, 3458, 387, 643, 899, 1411
                               , 1667, 2435, 2691, 1668, 2180, 133, 134, 136};
            ushort[] foodAmts = {180, 144, 120, 360, 72, 156, 96, 216, 12, 108,
                                      350, 60, 108, 24, 120, 108 };

            for (int i = 0; i < foodIDs.Length; i++) {
                del.Add(foodIDs[i], delegate(Item item, Player user, GameWorld world) {
                    user.AppendEatFood(foodAmts[i]);
                });
            }

            return null;
        }

        public Object blah(Object arg) {
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
                spell.RequiredMLevel = 8;
                spell.ManaCost = 100;
                spell.SpellEffect = MagicEffect.BLUE_SPARKLES;
                spell.VocationsFor = new Vocation[] { Vocation.SORCERER, Vocation.KNIGHT, Vocation.DRUID };
                spell.IsSpellValid = delegate(GameWorld world, string msg) {
                    return (player.GetItemCount(79, Constants.INV_RIGHT_HAND) > 0
                        || player.GetItemCount(79, Constants.INV_LEFT_HAND) > 0);
                };

                spell.Action = delegate(GameWorld world, Position hitPosition, List<Thing> hitBySpell) {
                    world.AppendBroadcast(player, argument);
                };
            };
            return null;
        }
    }
}

