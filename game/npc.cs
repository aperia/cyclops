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
    public delegate void NPCChatDelegate();

    public class NPC : Creature {
        private Creature talkingTo = null;
        private string lastMessage;
        private Creature lastCreatureSay;
        private static Dictionary<string, NPC> NPCDictionary = 
            new Dictionary<string,NPC>();
        private int talkTicks = 0; //Used to determine delay as to when to say bye message
        /// <summary>
        /// Gets the range in the specified direction from the starting position.
        /// </summary>
        /// <returns></returns>
        private int GetRange(Direction direction) {
            //Make sure the move is not out of 'range'
            //If so... just 'flip' the direction to get closer to the starting position
            switch (direction) {
                case Direction.NORTH:
                    return Math.Abs(Home.y - (CurrentPosition.y - 1));
                    /*if (currentRange > maxRadius) {
                        direction = Constants.SOUTH;
                    }*/
                    //break;
                case Direction.SOUTH:
                    return Math.Abs(Home.y - (CurrentPosition.y + 1));
                    /*if (currentRange > maxRadius) {
                        direction = Constants.NORTH;
                    }*/
                    //break;

                case Direction.EAST:
                    return Math.Abs(Home.x - (CurrentPosition.x + 1));
                    /*if (currentRange > maxRadius) {
                        direction = Constants.WEST;
                    }*/
                   // break;

                case Direction.WEST:
                    return Math.Abs(Home.x - (CurrentPosition.x - 1));
                    /*if (currentRange > maxRadius) {
                        direction = Constants.EAST;
                    }*/
                    //break;
                default:
                    throw new Exception("Invalid direction in GetRange()");
            }
        }

        public NPC() {
            Immunities = new ImmunityType[] 
            { ImmunityType.IMMUNE_ELECTRIC, ImmunityType.IMMUNE_FIRE, 
                ImmunityType.IMMUNE_PHYSICAL, ImmunityType.IMMUNE_POISON};
        }


        public Creature TalkingTo() {
            return talkingTo;
        }

        public int Radius {
            get;
            set;
        }

        public Position Home {
            get;
            set;
        }


        public NPCChatDelegate HandleMessage {
            get;
            set;
        }

        /// <summary>
        /// See base.GetAtkValue()
        /// </summary>
        /// <returns></returns>
        public override int GetAtkValue() {
            return 0;
        }

        /// <summary>
        /// See base.GetAtkValue()
        /// </summary>
        /// <returns></returns>
        public override int GetShieldValue() {
            return 0;
        }

        /// <summary>
        /// See base.GetChatType()
        /// </summary>
        /// <returns></returns>
        public override ChatLocal GetChatType() {
            return ChatLocal.SAY;
        }

        public override void AddLocalChat(ChatLocal chatType,
            string message, Position pos, Creature creatureFrom) {
                if (creatureFrom == this) {
                    return;
                }
                lastMessage = message.ToLower();
                lastMessage = lastMessage.Replace(".", "");
                lastMessage = lastMessage.Replace("?", "");
                lastCreatureSay = creatureFrom;
                HandleMessage();
        }

        public bool GetMessage(string msg) {
            msg = msg.Replace("$", "");
            msg = msg.ToLower();
            string[] words = Regex.Split(lastMessage, "\\s+");
            foreach (string word in words) {
                if (word.ToLower() == msg) {
                    return true;
                }
            }
            return false;
        //return lastMessage.Equals(msg);
        }


        public bool IsBusy() {
            return lastCreatureSay != talkingTo;
        }

        public bool IsIdle() {
            return talkingTo == null;
        }

        public void SetIdle() {
            talkingTo = null;
        }

        public string GetTalkingName() {
            return lastCreatureSay.Name;
        }

        public string Vanish {
            get;
            set;
        }

        public int Topic {
            get;
            set;
        }

        public int Price {
            get;
            set;
        }

        public ushort ItemType {
            get;
            set;
        }

        public int Amount {
            get;
            set;
        }

        private string talk; //todo: remove this lol

        public void AddChat(string message) {
            Topic = 0;
            message = message.Replace("%N", lastCreatureSay.Name);
            message = message.Replace("%T", GetTime());
            message = message.Replace("%P", Price.ToString());
            talk = message;
        }

        /// <summary>
        /// Used, instead, to get a random direction to move.
        /// Lol ;).
        /// </summary>
        /// <returns></returns>
        public override Spell GetSpell() {
            if (talkingTo != null) {
                if (!talkingTo.LogedIn || !CanSeeSameZ(talkingTo.CurrentPosition)) {
                    AddChat(Vanish);
                    SetIdle();
                } else {
                    TurnTowardsTarget(talkingTo);
                    CurrentWalkSettings.Destination = null;
                }
                return null;
            }

            Direction[] dirs = {Direction.EAST, Direction.SOUTH,
                                   Direction.WEST, Direction.NORTH};
            Direction dir = dirs[rand.Next(0, dirs.Length)];
            if (GetRange(dir) <= Radius) {
                CurrentWalkSettings.IntendingToReachDes = true;
                CurrentWalkSettings.Destination =
                    Position.GetNewPosition(CurrentPosition, dir);
            }
            return null;
        }

        public override string GetTalk() {
            if (!IsIdle()) {
                if (talkTicks++ >= 20) {
                    SetIdle();
                    return Vanish;
                }
                if (talkingTo == lastCreatureSay && talk != null) {
                    talkTicks = 0;
                }
            } else {
                talkTicks = 0;
            }

            string tmp = talk;
            talk = null;
            return tmp;
        }

        public void SetTalkingTo() {
            talkingTo = lastCreatureSay;
        }

        public string GetTime() {
            return "12:00";
        }

        /// <summary>
        /// Load all the npcs as specified in the configuration file.
        /// </summary>
        public static void Load() {
            string path = Config.GetDataPath() + Config.GetNPCDirectory();
            FileInfo[] fileList = new DirectoryInfo(path).GetFiles();
            DynamicCompile dCompile = new DynamicCompile();
            foreach (FileInfo info in fileList) {
                NPC npc = (NPC)dCompile.Compile(path + info.Name, null);
                NPCDictionary.Add(npc.Name.ToLower(), npc);
            }
        }

        /// <summary>
        /// Gets a list of all the npcs loaded.
        /// </summary>
        /// <returns>A list of all the NPCs.</returns>
        public static List<NPC> GetAllNPCs() {
            List<NPC> allNPCs = new List<NPC>();
            foreach (KeyValuePair<string, NPC> kvp in NPCDictionary) {
                allNPCs.Add(kvp.Value);
            }
            return allNPCs;
        }

        public override string GetLookAt(Player player) {
            return base.GetLookAt(player) + Name + ".";
        }

        public override int GetSpeed() {
            return BaseSpeed;
        }
    }
}
