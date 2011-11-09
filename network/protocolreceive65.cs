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
using System.Net.Sockets;

namespace Cyclops {
    /// <summary>
    ///  This is the class that handles all of the protocol receving
    ///  related information. It must encapsulate protocol receving
    ///  and therefore any protocol receving related information 
    ///  should only be stored here.
    /// </summary>
    public class ProtocolReceive65 : ProtocolReceive {
        private delegate void ProcessMessage(Player player, GameWorld world);
        private ProcessMessage[] messageDecoder;
        private const byte HEADER_MAX_VAL = 0xFF;
        private const byte MAX_STRING_LENGTH = 140;
        private Object lockThis;

        /// <summary>
        /// Checks to make sure a direction is valid.
        /// </summary>
        /// <param name="rawDirection">The direction as a byte.</param>
        /// <returns>True if the direction is valid, false otherwise.</returns>
        private bool IsDirectionValid(byte rawDirection) {
            return (Enum.IsDefined(typeof(Direction), rawDirection));
        }

        /// <summary>
        /// Process when a player closes a container.
        /// </summary>
        /// <param name="player">The player who closes the container.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessCloseContainer(Player player, GameWorld world) {
            byte localID = netmsg.GetByte();
            if (localID > Constants.MAX_CONTAINERS) {
                return;
            }
            world.HandleCloseContainer(player, localID);
        }

        /// <summary>
        /// Process when a player auto walks.
        /// </summary>
        /// <param name="player">The player who auto walked.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessAutoWalk(Player player, GameWorld world) {
            Position pos = netmsg.GetPosition();
#if DEBUG
            Log.WriteDebug("In ProcessAutoWalk()");
            Log.WriteDebug("pos: " + pos);
#endif
            world.HandleAutoWalk(player, pos);
        }

        /// <summary>
        /// Process when a player looks at something.
        /// </summary>
        /// <param name="player">The player who is looking.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessLookAt(Player player, GameWorld world) {
            //Position player is looking at
            Position pos = netmsg.GetPosition();
#if DEBUG
            Log.WriteDebug("In ProcessLookAt()");
            Log.WriteDebug("pos: " + pos);
#endif
            world.HandleLookAt(player, pos);
        }

        /// <summary>
        /// Process when a player changes fightMode or fightStance.
        /// </summary>
        /// <param name="player">The player changing the mode.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessChangeMode(Player player, GameWorld world) {
            byte rawFightMode = netmsg.GetByte(); //Agressive, normal, defensive
            byte rawFightStance = netmsg.GetByte(); //Follow, run away, stand still
            bool isFightModeValid = Enum.IsDefined(typeof(FightMode), rawFightMode);
            bool isFightStanceValid = Enum.IsDefined(typeof(FightStance), rawFightStance);

            //Make sure modes are valid
            if (!isFightModeValid || !isFightStanceValid) {
                return;
            }

            FightMode fightMode = (FightMode)rawFightMode;
            FightStance fightStance = (FightStance)rawFightStance;
#if DEBUG
            Log.WriteDebug("In ProcessChangeMode()");
            Log.WriteDebug("fightMode: " + fightMode);
            Log.WriteDebug("fightStance: " + fightStance);
#endif
            world.HandleChangeMode(player, fightMode, fightStance);
        }

        /// <summary>
        /// Process when a player leaves his battle screen.
        /// </summary>
        /// <param name="player">The player who exits his battle screen.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessExitBattle(Player player, GameWorld world) {
#if DEBUG
            Log.WriteDebug("In ProcessExitBattle()");
#endif
            world.HandleExitBattle(player);
        }

        /// <summary>
        /// Process when a player uses an item.
        /// </summary>
        /// <param name="player">The player who uses the item.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessUseItem(Player player, GameWorld world) {
            byte itemType = netmsg.GetByte(); //1 = regular, 2 = usable with
            Position pos = netmsg.GetPosition();
            ushort itemID = netmsg.GetU16();
            byte stackpos = netmsg.GetByte();

            byte unknownByte = netmsg.GetByte(); //unknown?? TODO: Figure out

#if DEBUG
            Log.WriteDebug("In ProcessUseItem()");
            Log.WriteDebug("itemType: " + itemType);
            Log.WriteDebug("pos: " + pos);
            Log.WriteDebug("itemID: " + itemID);
            Log.WriteDebug("stackpos: " + stackpos);
            Log.WriteDebug("unknownbyte: " + unknownByte);
#endif

            if (itemType == Constants.ITEM_TYPE_USE) {
                world.HandleUseItem(player, itemType, pos, itemID, stackpos);
            } else if (itemType == Constants.ITEM_TYPE_USE_WITH) {
                Position posWith = netmsg.GetPosition();
                ushort spriteIDWith = netmsg.GetU16();
                byte stackposWith = netmsg.GetByte();
#if DEBUG
                Log.WriteDebug("posWith: " + posWith);
                Log.WriteDebug("spriteid with: " + spriteIDWith);
                Log.WriteDebug("stackPos: " + stackposWith);

#endif
                world.HandleUseItemWith(player, itemType, pos, itemID,
                    stackpos, posWith, stackposWith);
            }



            /*if (Item.CreateItem(itemID).IsOfType(Constants.TYPE_USEABLE_WITH)) {
                Position posWith = netmsg.GetPosition();
                world.SendPlayerUseItem(p, pos, itemID, stackpos, posWith);
            } else {
                world.SendPlayerUseItem(p, pos, itemID, stackpos, null);
            }*/
        }

        /// <summary>
        /// Process when a player says something.
        /// </summary>
        /// <param name="player">The player who is talking.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessChat(Player player, GameWorld world) {
            string msg; //Thing that the player is saying
            msg = netmsg.GetStringL();

            //Test for acceptable string length
            if (msg.Length > MAX_STRING_LENGTH) {
                return;
            }
#if DEBUG
            Log.WriteDebug("In ProcessChat()");
            Log.WriteDebug("msg: " + msg);
#endif
            world.HandleChat(player, msg);
        }

        /// <summary>
        /// Processes a client "bounce back" message.
        /// </summary>
        /// <param name="player">The client's player who sent the message.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessBounceBack(Player player, GameWorld world) {
#if DEBUG
            Log.WriteDebug("In ProcessBounceBack()");
#endif
            world.HandleBounceBack(player);
        }

        /// <summary>
        /// Process a player's push.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessPush(Player player, GameWorld world) {
            Position posFrom = netmsg.GetPosition();
            ushort thingID = netmsg.GetU16();
            byte stackpos = netmsg.GetByte();
            Position posTo = netmsg.GetPosition();
            byte count = netmsg.GetByte();
#if DEBUG
            Log.WriteDebug("In ProcessPush()");
            Log.WriteDebug("posFrom: " + posFrom);
            Log.WriteDebug("thingID: " + thingID);
            Log.WriteDebug("stackpos: " + stackpos);
            Log.WriteDebug("posTo: " + posTo);
            Log.WriteDebug("count: " + count);
#endif
            if (count == 0) {
                return;
            }
            world.HandlePush(player, posFrom, thingID, stackpos, posTo, count);
        }

        /// <summary>
        /// Process a player's walk.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessWalk(Player player, GameWorld world) {
            byte rawDirection = netmsg.GetByte();

            //Validate direction
            if (!IsDirectionValid(rawDirection))
                return;

            Direction direction; //direction to move

            direction = (Direction)rawDirection;
#if DEBUG
            Log.WriteDebug("In ProcessWalk()");
            Log.WriteDebug("direction: " + direction);
#endif
            world.HandleManualWalk(player, direction);

            /* p.SetTargetPosition(null);

             if (p.GetLastWalk().Elapsed()) //Enough time passed
                 world.SendCreatureWalk(p, direction);*/
        }

        /// <summary>
        /// Proccess a player's change direction.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessChangeDirection(Player player, GameWorld world) {
            byte rawDirection = netmsg.GetByte();

            //Validate direction
            if (!IsDirectionValid(rawDirection))
                return;

            Direction direction; //direction to move

            direction = (Direction)rawDirection;
#if DEBUG
            Log.WriteDebug("In ProcessChangeDirection()");
            Log.WriteDebug("direction: " + direction);
#endif
            world.HandleChangeDirection(player, direction);
        }

        /// <summary>
        /// Process a player's request on changing outfit.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessRequestOutfit(Player player, GameWorld world) {
            world.HandleRequestOutfit(player);
        }

        /// <summary>
        /// Process setting an outfit.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessSetOutfit(Player player, GameWorld world) {
            byte upper = netmsg.GetByte();
            byte middle = netmsg.GetByte();
            byte lower = netmsg.GetByte();
#if DEBUG
            Log.WriteDebug("In ProcessSetOutfit()");
            Log.WriteDebug("Upper: " + upper);
            Log.WriteDebug("Middle: " + middle);
            Log.WriteDebug("Lower: " + lower);
            
#endif
            world.HandleSetOutfit(player, lower, middle, upper);
        }


        /// <summary>
        /// Process a player's set text (for example, write text on a scroll).
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessSetText(Player player, GameWorld world) {
            uint windowID = netmsg.GetU32();
            string txt = netmsg.GetStringL();
        }

        /// <summary>
        /// Process a player's house text submission.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessHouseText(Player player, GameWorld world) {
            byte type = netmsg.GetByte();
            uint windowID = netmsg.GetU32();
            string txt = netmsg.GetStringL();
        }

        /// <summary>
        /// Process a player's change outfit.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessChangeOutfit(Player player, GameWorld world) {
            uint newOutfit;

            newOutfit = netmsg.GetU32();
            //world.SendPlayerChangeOutfit(p, newOutfit);
        }

        /// <summary>
        /// Process when a player sends a comment.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessComment(Player player, GameWorld world) {
            string comment = netmsg.GetStringZ();
            FileHandler fileHandler = new FileHandler();
            lock (lockStatic) {
                fileHandler.SaveComment(comment, player);
            }
            player.ResetNetMessages();
            player.AddAnonymousChat(ChatAnonymous.WHITE,
                "Your comment has been submitted");
            player.WriteToSocket();
        }

        /// <summary>
        /// Process when a player attacks a target.
        /// </summary>
        /// <param name="player">The attacker.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessSetTarget(Player player, GameWorld world) {
            uint targetID = netmsg.GetU32();
            world.HandleCreatureTarget(player, targetID);
        }

        /// <summary>
        /// Process a player's logout.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessLogout(Player player, GameWorld world) {
#if DEBUG
            Log.WriteDebug("In ProcessLogout()");
#endif
            world.HandleLogout(player);
        }


        /// <summary>
        /// Initialize the decoder for processing player's header messages.
        /// </summary>
        private void InitDecoder() {
            messageDecoder[0x05] = ProcessWalk;
            messageDecoder[0x06] = ProcessAutoWalk;
            messageDecoder[0x07] = ProcessLookAt;
            messageDecoder[0x09] = ProcessChat;
            messageDecoder[0x0A] = ProcessChangeDirection;
            messageDecoder[0x0B] = ProcessComment;
            messageDecoder[0x14] = ProcessPush;
            messageDecoder[0x1E] = ProcessUseItem;
            messageDecoder[0x1F] = ProcessCloseContainer;
            messageDecoder[0x20] = ProcessRequestOutfit;
            messageDecoder[0x21] = ProcessSetOutfit;
            messageDecoder[0x23] = ProcessSetText;
            messageDecoder[0x24] = ProcessHouseText;
            messageDecoder[0x32] = ProcessChangeMode;
            messageDecoder[0x33] = ProcessExitBattle;
            messageDecoder[0x34] = ProcessSetTarget;
            messageDecoder[0xC8] = ProcessBounceBack;
            messageDecoder[0xFF] = ProcessLogout;
        }

        /// <summary>
        /// Handle a player's login connection attempt.
        /// </summary>
        /// <param name="s">The socket fo</param>
        /// <returns></returns>
        public override LoginInfo HandlePlayerLogin(Socket s) {
            netmsg.ReadFromSocket();
            netmsg.SkipBytes(5); //Client bytes, useless to us
            uint version = netmsg.GetU16(); //Client version [TODO: Check if client version is correct?]
            string username = netmsg.GetStringZ(30);
            string password = netmsg.GetStringZ();
            return new LoginInfo(username, password);
        }

        /// <summary>
        /// Constructor object used to initialize a ProtocolReceive object.
        /// </summary>
        public ProtocolReceive65(Socket socket) {
            netmsg = new NetworkMessage(socket);

            //+1 because it is 0-based
            messageDecoder = new ProcessMessage[HEADER_MAX_VAL + 1];
            InitDecoder();
            lockThis = new Object();
        }

        public Player CurrentPlayer {
            get;
            set;
        }

        public GameWorld CurrentWorld {
            get;
            set;
        }


        /// <summary>
        /// Attemps to read from the socket or blocks until
        /// a message is ready to be read and if successful, it 
        /// processes that message.
        /// </summary>
        /// <param name="player">Player doing the action.</param>
        /// <param name="world">A reference to the game world.</param>
        /// <returns>Returns true if to continue processing messages, 
        /// false otherwise.</returns>
        private bool ProcessNextMessage(Player player, GameWorld world) {
            //TODO: Handle logging out disposed error... ;/
            if (!netmsg.Connected()) {
                return false;
            }
            netmsg.Reset();
            ushort header = netmsg.GetU16();

            if (header > HEADER_MAX_VAL) {
                throw new Exception("Invalid header sent. Header: " + header);
            }

            //Only process loged in player's messages, unless player
            //wants to log out
            if (!world.IsCreatureLogedIn(player)) {
                if (header == 0xFF) { //Logout header
                    player.Logout();
                    return false;
                }
            } else if (messageDecoder[header] == null) {
                PrintHeader(netmsg, header);
            } else {
                messageDecoder[header].Invoke(player, world);
            }
            return true;
        }

        
        /*public override Account HandleAccountLogin(NetworkMessage networkmsg) {
            throw new NotImplementedException();
            //TODO: Finish
            byte clientOS = networkmsg.GetByte();
            ushort protocolID = networkmsg.GetU16();
            //if (protocolID != 650 && protocolID != 710) {
            //    return null;
            //}
            uint actNumber = networkmsg.GetU32();
            string pw = networkmsg.GetStringL();
            return Account.Load(actNumber, pw);
        }*/

        /// <summary>
        /// Stars listening asynchronously and handles the messages as needed. 
        /// </summary>
        public override void StartReceiving(GameWorld world, Player player) {
            CurrentWorld = world;
            CurrentPlayer = player;
            netmsg.BeginReceiving(new AsyncCallback(HandleReceive));

        }

        private void HandleReceive(IAsyncResult ar) {
            if (!netmsg.Connected())
                return;

            int bytes = netmsg.EndReceive(ar);
            ProcessNextMessage(CurrentPlayer, CurrentWorld);
            if (bytes > 0) {
                netmsg.BeginReceiving(new AsyncCallback(HandleReceive));
            }
        }

    }
}
