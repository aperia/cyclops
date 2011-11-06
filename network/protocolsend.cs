using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyclops {
    /// <summary>
    /// Used for abstracting the protocol. Used
    /// in order to support multiple protocols.
    /// </summary>
    public abstract class ProtocolSend {
        protected NetworkMessage netmsg;

        /// <summary>
        /// Gets the z iterator relative to the player.
        /// </summary>
        /// <param name="startZ">The z to start at.</param>
        /// <param name="endZ">The z to end at.</param>
        /// <param name="zStep">Which direction to move the z iterator.</param>
        /// <param name="z">Player's z position.</param>
        protected static void GetZIter(ref short startZ, ref short endZ,
            ref short zStep, byte z) {
            if (z > 7) {
                startZ = (byte)(z - 2);
                endZ = Math.Min((short)(Constants.MAP_MAX_LAYERS - 1), (short)(z + 2));
                zStep = 1;
            } else {
                startZ = 7;
                endZ = 0;
                zStep = -1;
            }
        }


        public abstract void Reset();
        public abstract void RemoveThing(Position pos, byte stackpos);
        public abstract void AddScreenCreature(Creature creature, bool knowsCreature,
            Position pos, byte stackpos);
        public abstract void UpdateItem(Position pos, Item item, byte stackpos);
        public abstract void AddItem(Position pos, Item item, byte stackpos);
        public abstract void AddTileCreature(Creature creature, bool knowsCreature);
        public abstract void AddStatusMessage(string msg);
        public abstract void UpdateWorldLight(byte light);
        public abstract void UpdateCreatureHealth(Creature creature);
        public abstract void UpdateCreatureLight(Creature creature, byte light);
        public abstract void UpdateCreatureOutfit(Creature creature);
        public abstract void AddFullInventory(Player player);
        public abstract void AddCarryingItem(Item item);
        public abstract void AddGroundItem(Item item);
        public abstract void AddLoginBytes(Player player, Map map);
        public abstract void AddSorryBox(string message);
        public abstract void AddForYourInformationBox(string message);
        public abstract void AddRequestOutfit(Creature creature);
        public abstract void AddMOTD(string message, ushort messageNumber);
        public abstract void AddScreenMap(Map map, Player player);
        public abstract void AddScreenMoveByOne(Direction direction, Position oldPos,
            Position newPos, Map map, Player player);
        public abstract void AddContainerClose(byte localID);
        public abstract void AddContainerOpen(byte localID, Container cont);
        public abstract void UpdateOpenContainer(byte localID, Container cont);
        public abstract void AddInventoryItem(byte index, Item invItem);
        public abstract void RemoveInventoryItem(byte index);
        
        /* DEPRECATED */
        public abstract void UpdateCarryingItem(byte localID, UpdateCarryingType type,
            Item newItem);

        public abstract void AddEffect(MagicEffect effect, Position pos);
        public abstract void AddShootEffect(byte effect, Position origin, Position dest);
        public abstract void AddItemText(ushort itemID, uint windowID, ushort length, string message);
        public abstract void AddHouseText(uint windowID, ushort length, string message);
        public abstract void AddStats(Player player);
        public abstract void AddSkills(Player player);
        public abstract void AddCreatureMove(Direction direction, Creature creature,
            Position oldPos, Position newPos, byte oldStackpos, byte newStackpos);
        public abstract void UpdateCreatureDirection(Creature creature,
            Direction direction, byte stackpos, Position position);
        public abstract void AddLocalChat(ChatLocal chatType, string message, Position pos,
            string nameFrom);
        public abstract void AddGlobalChat(ChatGlobal chatType, string message,
            string nameFrom);
        public abstract void AddAnonymousChat(ChatAnonymous chatType, string message);
        public abstract void AddUserList(List<string> names);
        public abstract void AddUserInfo(string message);
        public abstract void AddBounceBack();
        public abstract void WriteToSocket();
        public virtual void WriteToSocket(bool closeSocket) {
        }

        /// <summary>
        /// Mark the socket as closed so on the next write (because
        /// of the asynchronous nature of I/O), the socket will be closed.
        /// </summary>
        public virtual void MarkSocketAsClosed() {
            netmsg.MarkSocketClosed = true;
        }

        /// <summary>
        /// Forcibly close the socket immediately.
        /// </summary>
        public abstract void Close();

        public abstract void AddCharacterList(List<string> chars, string gameWorld,
            uint IP, ushort port, ushort premDays);
    }
}
