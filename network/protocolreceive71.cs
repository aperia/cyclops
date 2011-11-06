using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Cyclops {
    public class ProtocolReceive71 : ProtocolReceive {
        private delegate void ProcessMessage(Player player, GameWorld world);
        private ProcessMessage[] messageDecoder;
        private const byte HEADER_MAX_VAL = 0xFF;
        private const byte MAX_STRING_LENGTH = 140;

        /// <summary>
        /// Process when a player says something.
        /// </summary>
        /// <param name="player">The player who is talking.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessChat(Player player, GameWorld world) {
            byte speakType = netmsg.GetByte();
            string reciever = null;
            ushort channelID = 0;
            if (speakType == Constants.SPEAK_PRIVATE_MSG || 
                speakType == Constants.SPEAK_RV_COUNSELLOR) {
                reciever = netmsg.GetStringL();
                return;
            } else if (speakType == Constants.SPEAK_CHANNEL_YELLOW) {
                channelID = netmsg.GetU16();
                return;
            }

            string msg = netmsg.GetStringL();
            Console.WriteLine("msg: " + msg);
            //Test for acceptable string length
            if (msg.Length > MAX_STRING_LENGTH) {
                return;
            }

            world.HandleChat(player, msg);
        }

        /// <summary>
        /// Process a player's walk.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessWalkNorth(Player player, GameWorld world) {
            world.HandleManualWalk(player, Direction.NORTH);
        }

        /// <summary>
        /// Process a player's walk.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessWalkEast(Player player, GameWorld world) {
            world.HandleManualWalk(player, Direction.EAST);
        }

        /// <summary>
        /// Process a player's walk.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessWalkSouth(Player player, GameWorld world) {
            world.HandleManualWalk(player, Direction.SOUTH);
        }

        /// <summary>
        /// Process a player's walk.
        /// </summary>
        /// <param name="player">The player doing the action.</param>
        /// <param name="world">A reference to the gameworld.</param>
        private void ProcessWalkWest(Player player, GameWorld world) {
            world.HandleManualWalk(player, Direction.WEST);
        }

        public override LoginInfo HandlePlayerLogin(Socket s) {
            byte clientOS = netmsg.GetByte();
            ushort version = netmsg.GetU16();
            byte isGM = netmsg.GetByte();
            string playerName = netmsg.GetStringL();
            string playerPW = netmsg.GetStringL();
            return new LoginInfo(playerName, playerPW);
        }

        /// <summary>
        /// Initialize the decoder for processing player's header messages.
        /// </summary>
        private void InitDecoder() {
            messageDecoder[0x65] = ProcessWalkNorth;
            messageDecoder[0x66] = ProcessWalkEast;
            messageDecoder[0x67] = ProcessWalkSouth;
            messageDecoder[0x68] = ProcessWalkWest;
            messageDecoder[0x96] = ProcessChat;
        }


        /// <summary>
        /// Constructor object used to initialize a ProtocolReceive object.
        /// </summary>
        /// <param name="networkmsg">A reference to the NetworkMessage instance being
        /// used.</param>
        public ProtocolReceive71(NetworkMessage networkmsg) {
            netmsg = networkmsg;

            //+1 because it is 0-based
            messageDecoder = new ProcessMessage[HEADER_MAX_VAL + 1];
            InitDecoder();
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
       /* public bool ProcessNextMessage(Player player, GameWorld world) {
            //TODO: Handle logging out disposed error... ;/
            if (!netmsg.Connected()) {
                return false;
            }

            netmsg.ReadFromSocket();
            byte header = netmsg.GetByte();

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
              //  PrintHeader(netmsg, header);
            } else {

                messageDecoder[header](player, world);
            }
            return true;
        }*/

        /*public override Account HandleAccountLogin(NetworkMessage networkmsg) {
            byte clientOS = networkmsg.GetByte();
            ushort protocolID = networkmsg.GetU16();
            networkmsg.SkipBytes(12);
            if (protocolID != 650 && protocolID != 710) {
                return null;
            }
            uint actNumber = networkmsg.GetU32();
            string pw = networkmsg.GetStringL();
            return Account.Load(actNumber, pw);
        }*/
    }
}
