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

namespace Cyclops {
    /// <summary>
    /// This class handles chatting for the game server.
    /// </summary>
    class ChatSystem {
        private const string EMPTY_SPACE = " ";
        private const string PRIVATE_MSG_INDICATOR = "*";
        private const string MSG_QUANTIFIER = "#";
        private const char BROADCAST_TYPE = 'b';
        private const char WHISPER_TYPE = 'w';
        private const char YELL_TYPE = 'y';

        private Map gameMap;
        private Dictionary<string, Player> playersOnline;

        /// <summary>
        /// Handles a broadcast made by a creature.
        /// </summary>
        /// <param name="sender">The sender of the broadcast.</param>
        /// <param name="msg">The message to broadcast.</param>
        private void HandleBroadcast(Creature sender, string msg) {
            if (!msg.ToLower().StartsWith(BROADCAST_TYPE + "")) {
                throw new Exception("Invalid call to HandleBroadcast()");
            }
            msg = msg.Substring(1); //Remove quantifier type

            //Invalid access level
            if (sender.Access < Constants.ACCESS_GAMEMASTER) {
                return;
            }

            foreach (KeyValuePair<string, Player> kvp in playersOnline) {
                kvp.Value.ResetNetMessages();
                kvp.Value.AddGlobalChat(ChatGlobal.BROADCAST, msg, sender.Name);
                kvp.Value.WriteToSocket();
            }
        }

        /// <summary>
        /// A quantified message is a message that starts with a special character. For example,
        /// in tibia 6.4, # is a quantifier, so #y is used to yell, #w is used to whisper etc.
        /// </summary>
        /// <param name="sender">Sender of the quantified message.</param>
        /// <param name="msg">The message itself.</param>
        private void HandleQuantifiedMessage(Creature sender, string msg, ThingSet tSet) {
            if (!msg.StartsWith(MSG_QUANTIFIER)) {
                throw new Exception("Invalid call to HandleQuantifiedMessage()");
            }
            msg = msg.Substring(1); //Remove first quanitifer
            char quantifierType = char.ToLower(msg[0]);
            switch (quantifierType) {
                case BROADCAST_TYPE:
                    HandleBroadcast(sender, msg);
                    break;
                case WHISPER_TYPE:
                     gameMap.GetThingsInWhisperVicinity(sender.CurrentPosition, tSet);
                    //msg.Substring(2) is called in order to remove quantifier type and a space
                     HandleLocalChat(ChatLocal.WHISPER, tSet, msg.Substring(2), sender);

                    break;
                case YELL_TYPE:
                    gameMap.GetThingsInYellVacinity(sender.CurrentPosition, tSet);
                    //msg.Substring(2) is called in order to remove quantifier type and a space
                    HandleLocalChat(ChatLocal.YELLS, tSet, msg.Substring(2).ToUpper(), sender);
                    break;
                default:
#if DEBUG
                    Log.WriteDebug("Invalid quantifier type");
#endif
                    break;
            }
        }

        /// <summary>
        /// This method handles a private message.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="msg">The message itself.</param>
        private void HandlePrivateMessage(Creature sender, string msg) {
            // Valid format: *name* msg 

            if (!msg.StartsWith(PRIVATE_MSG_INDICATOR)) {
                throw new Exception("Invalid call to HandlePrivateMessage()");
            }

            bool isName = true;
            string name = "";
            string message = "";
            for (int i = 1; i < msg.Length; i++) {
                string charAt = msg[i] + "";
                if (charAt == PRIVATE_MSG_INDICATOR) {
                    isName = false;
                    continue; //Skip adding PRIVATE_MSG_INDICATOR to message
                }

                if (isName) {
                    name += charAt;
                } else {
                    message += charAt;
                }
            }

            //Remove starting space, if applicable;
            if (message.StartsWith(EMPTY_SPACE)) {
#if DEBUG
                Log.WriteDebug("Message started with empty space.");
#endif
                message = message.Substring(1);
            }

            AppendPrivateMessage(sender, name, message);
        }

        /// <summary>
        /// Handles sending a chat message in a player's local vicinity.
        /// </summary>
        /// <param name="type">The type of chat to send.</param>
        /// <param name="set">The set of things in the player's local vicinity.</param>
        /// <param name="msg">The message to send.</param>
        /// <param name="sender">The sender of the message.</param>
        private void HandleLocalChat(ChatLocal type, ThingSet set, 
            string msg, Creature sender) {

            foreach (Thing thing in set.GetThings()) {
                thing.AddLocalChat(type, msg, sender.CurrentPosition, sender);
            }
        }

        /// <summary>
        /// Sends a private message to another player.
        /// </summary>
        /// <param name="sender">The creature sending.</param>
        /// <param name="recipient">The recipient's name (player).</param>
        /// <param name="msg">The message to send.</param>
        private void AppendPrivateMessage(Creature sender, string recipient, string msg) {
            recipient = recipient.ToLower();
            foreach (KeyValuePair<string, Player> kvp in playersOnline) {
                if (kvp.Key.StartsWith(recipient)) {
                    kvp.Value.AddGlobalChat(ChatGlobal.PRIVATE_MSG, msg, sender.Name);
                    sender.AddStatusMessage("Message sent to " + kvp.Value.Name + ".");
                    return;
                }
            }
            sender.AddStatusMessage("A player with this name is not online.");
        }

        /// <summary>
        /// Used to initialize the chat system.
        /// </summary>
        /// <param name="mapRef">A reference to the game map.</param>
        /// <param name="playersOnlineRef">A reference to the players online.</param>
        public ChatSystem(Map mapRef, Dictionary<string, Player> playersOnlineRef) {
            gameMap = mapRef;
            playersOnline = playersOnlineRef;
        }

        /// <summary>
        /// Handle a message from a creature.
        /// </summary>
        /// <param name="creature">The creature sending the message.</param>
        /// <param name="msg">The message sent.</param>
        public void HandleChat(Creature creature, string msg) {
            ThingSet tSet = new ThingSet();
            if (msg.StartsWith(PRIVATE_MSG_INDICATOR)) {
                HandlePrivateMessage(creature, msg);
            } else if (msg.StartsWith(MSG_QUANTIFIER)) {
                HandleQuantifiedMessage(creature, msg, tSet);
            } else {
                tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition, true);
                HandleLocalChat(creature.GetChatType(), tSet, msg, creature);
            }
        }
    }
}
