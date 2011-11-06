using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyclops {
    public class ProtocolSend71 : ProtocolSend {
        private Dictionary<int, int> conversion;

        private void AddSpeak(string from, Position pos, string msg, byte type,
            ushort? ChannelID) {
                netmsg.AddByte(0xAA);
                netmsg.AddStringL(from);
                netmsg.AddByte(type);
           // if (ChannelID != null) {

        }

        /// <summary>
        /// Adds the map information as specified by the parameters.
        /// </summary>
        /// <param name="x">Center of map relative to client.</param>
        /// <param name="y">Center to map relative to client.</param>
        /// <param name="z">Center to map relative to client.</param>
        /// <param name="width">Width of map to send.</param>
        /// <param name="height">Height of map to send.</param>
        /// <param name="map">A reference to the gamemap.</param>
        /// <param name="player">The player for whom the
        /// map information is sent.</param>
        private void AddMapInfo(int x, int y,
            byte z, int width, int height, Map map, Player player) {
            AddMapInfo(new Position((ushort)x, (ushort)y,
                z), width, height, map, player);
        }
        /// <summary>
        /// Adds the a z level of a map.
        /// </summary>
        /// <param name="pos">The floor's center position.</param>
        /// <param name="width">Floor weight to add.</param>
        /// <param name="height">Floor height to add.</param>
        /// <param name="offset">Floor offset relative to the player.</param>
        /// <param name="map">A reference to the map.</param>
        /// <param name="player">The player for whom the map information is sent.</param>
        private void AddFloorInfo(Position pos, int width, int height,
             short offset, Map map, Player player, ref short skipTiles) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Tile tile = 
                        map.GetTile(x + pos.x + offset, y + pos.y + offset, pos.z);
                    if (tile != null) {
                        if (skipTiles >= 0) {
                            netmsg.AddByte((byte)skipTiles);
                            netmsg.AddByte(0xFF);
                        }
                        skipTiles = 0;
                        AddMapTile(tile, player);
                    } else {
                        skipTiles++;
                        if (skipTiles == 0xFF) {
                            netmsg.AddByte(0xFF);
                            netmsg.AddByte(0xFF);
                            skipTiles = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single map tile.
        /// </summary>
        /// <param name="tile">Maptile to add.</param>
        /// <param name="player">Player for whom the map tile is being added.</param>
        private void AddMapTile(Tile tile, Player player) {
            if (tile != null) {
                foreach (Thing t in tile.GetThings()) {
                    t.AddItself(this, player);
                }
            }
        }

        /// <summary>
        /// Adds the entire map.
        /// </summary>
        /// <param name="pos">Map center position.</param>
        /// <param name="map">A reference to the map.</param>
        /// <param name="player">The player for whom 
        /// the map information is being sent.</param>
        private void AddFullMap(Position pos, Map map, Player player) {
            Position startPos = new Position();
            startPos.x = (ushort)(pos.x - 8);
            startPos.y = (ushort)(pos.y - 6);
            startPos.z = pos.z;
            AddMapInfo(startPos, 18, 14, map, player);
        }

        /// <summary>
        /// Adds the map information as specified by the parameters.
        /// </summary>
        /// <param name="pos">Center of the map position.</param>
        /// <param name="width">Width of the map.</param>
        /// <param name="height">Height of the map.</param>
        /// <param name="map">A reference to the map.</param>
        /// <param name="player">The player for whom the 
        /// map information is sent.</param>
        private void AddMapInfo(Position pos, int
            width, int height, Map map, Player player) {

            short skipTiles = -1;
            
            short startZ = 0, endZ = 0, zStep = 0;
            GetZIter(ref startZ, ref endZ, ref zStep, pos.z);
            for (short iterZ = startZ; iterZ != endZ + zStep; iterZ += zStep) {
                Position posTmp = new Position(pos.x, pos.y, (byte)iterZ);
                AddFloorInfo(posTmp, width, height, (short)(pos.z - iterZ),
                    map, player, ref skipTiles);
            }

             if (skipTiles >= 0)
            {
                netmsg.AddByte((byte)skipTiles);
                netmsg.AddByte(0xFF);
            }
        }


        private void AddItemRaw(Item item) {
            //TODO: Finish this code
            netmsg.AddU16((ushort)conversion[item.ItemID]);
            if (item.IsOfType(Constants.TYPE_FLUID_CONTAINER)) {
                netmsg.AddByte(0x00);
            }
            if (item.IsOfType(Constants.TYPE_STACKABLE)) {
                netmsg.AddByte(0x00);
            }
        }

        public ProtocolSend71(NetworkMessage networkmsg) {
            netmsg = networkmsg;
            conversion = ItemConversion.GetConversionDictionary();
        }

        public void AddTextMessage(byte type, string message) {
            netmsg.AddByte(0xB4);
            netmsg.AddByte(type);
            netmsg.AddStringL(message);
        }

        public override void AddAnonymousChat(ChatAnonymous chatType, string message) {
            if (chatType == ChatAnonymous.GREEN) {
                AddTextMessage(Constants.MSG_GREEN, message);
            } else if (chatType == ChatAnonymous.WHITE) {
                AddTextMessage(Constants.MSG_WHITE, message);
            }
        }
        public override void AddFullInventory(Player player) {
            for (byte i = 1; i <= 10; i++) { //TODO: Fix hard codec constants
                if (player.GetInventoryItem(i) != null) {
                    netmsg.AddByte(0x78);
                    netmsg.AddByte(i);
                   // AddItem(player.GetInventoryItem(i)[i],
                   //     player.gameServRef.gameMap.getItemDesc(player.inventory[i]));
                } else {
                    netmsg.AddByte(0x79);
                    netmsg.AddByte((byte)i);
                }
            }
        }

        public override void AddSkills(Player player) {
            netmsg.AddByte(0xA1);
            for (int i = 0; i < 7; i++) {
                netmsg.AddByte(player.GetSkill(i));
            }
        }

        public override void AddStats(Player player) {
            netmsg.AddByte(0xA0);
            netmsg.AddU16(player.CurrentHP); //currentHealth
            netmsg.AddU16(player.MaxHP); //Max health
            netmsg.AddU16(player.MaxCapacity);             // cap
            netmsg.AddU32(player.Experience);                // experience
            netmsg.AddByte(player.Level);              // level
            netmsg.AddU16(player.CurrentMana);      // currentMana
            netmsg.AddU16(player.MaxMana);   // maxMana
            netmsg.AddByte(player.MagicLevel);               // maglevel
        }
        public override void AddItem(Position pos, Item item, byte stackpos) {
            netmsg.AddByte(0x6A);
            netmsg.AddPosition(pos);
            AddItemRaw(item);
        }
        public override void RemoveThing(Position pos, byte stackpos) {
            netmsg.AddByte(0x6C);
            netmsg.AddPosition(pos);
            netmsg.AddByte(stackpos);
        }

        public override void UpdateItem(Position pos, Item item, byte stackpos) {
            if (stackpos < 10) {
                netmsg.AddByte(0x6B);
                netmsg.AddPosition(pos);
                netmsg.AddByte(stackpos);
                AddItemRaw(item);
            }
        }

        public override void UpdateCreatureLight(Creature creature, byte light) {
            netmsg.AddByte(0x8D);
            netmsg.AddU32(creature.GetID());
            netmsg.AddByte(creature.SpellLightLevel);
            netmsg.AddByte(0xD7); //Light color
        }

        public override void AddTileCreature(Creature creature, bool knowsCreature) {
            if (knowsCreature) {
                netmsg.AddByte(0x62);
                netmsg.AddByte(0x00);
                netmsg.AddU32(creature.GetID());
            } else {
                netmsg.AddByte(0x61);
                netmsg.AddByte(0x00);

                netmsg.AddU32(0); //TODO: Login stuff?
                netmsg.AddU32(creature.GetID());
                netmsg.AddStringL(creature.Name);
            }

            netmsg.AddByte(creature.GetHealthPercentage()); //HP percent
            netmsg.AddByte((byte)creature.CurrentDirection); //Direction

            /*if (creature.isInvisible) {
                AddCreatureInvisible();
            } else if (creature.isUsingChameleon) {
                AddByte(0x00);
                AddU16((ushort)creature.chameleonItem);
            } else {*/
            netmsg.AddByte(0x7F); //lookhead
            netmsg.AddByte(0x00);
            netmsg.AddByte(0x00);
            netmsg.AddByte(0x00);
            netmsg.AddByte(0x00);

            //netmsg.AddByte(creature.CharType);
            /*
            netmsg.AddByte(creature.colorHead);
            netmsg.AddByte(creature.colorBody);
            netmsg.AddByte(creature.colorLegs);
            netmsg.AddByte(creature.colorFeet);
             */ //TODO: FINISH THIS
           // }

            netmsg.AddByte(creature.SpellLightLevel); //light levels
            netmsg.AddByte(0xD7); // light color, yellow light = 0xD7

            netmsg.AddU16((ushort)creature.GetSpeed());  //speed
        }

        public override void UpdateCreatureHealth(Creature creature) {
            netmsg.AddByte(0x8C);
            netmsg.AddU32(creature.GetID());
            netmsg.AddByte(creature.GetHealthPercentage());
        }

        public override void AddShootEffect(byte effect, Position origin, Position dest) {
            netmsg.AddByte(0x85);
            netmsg.AddPosition(origin);
            netmsg.AddPosition(dest);
            netmsg.AddByte(effect);
        }

        public override void AddEffect(MagicEffect effect, Position pos) {
            netmsg.AddByte(0x83);
            netmsg.AddPosition(pos);
            netmsg.AddByte((byte)effect);
        }
        public override void AddSorryBox(string message) {
            netmsg.AddByte(0x0A);
            netmsg.AddStringL(message);
        }
        public override void AddBounceBack() {
           // throw new NotImplementedException();
        }
        public override void AddCarryingItem(Item item) {
            throw new NotImplementedException();
        }
        public override void AddContainerClose(byte localID) {
            throw new NotImplementedException();
        }
        public override void AddContainerOpen(byte localID, Container cont) {
            throw new NotImplementedException();
        }
        public override void AddCreatureMove(Direction direction, Creature creature, Position oldPos, Position newPos, byte oldStackpos, byte newStackpos) {
            throw new NotImplementedException();
        }
        public override void AddForYourInformationBox(string message) {
            throw new NotImplementedException();
        }
        public override void AddGlobalChat(ChatGlobal chatType, string message, string nameFrom) {
            if (chatType == ChatGlobal.BROADCAST) {

            }
        }
        public override void AddGroundItem(Item item) {
            AddItemRaw(item); ;
        }
        public override void AddHouseText(uint windowID, ushort length, string message) {
            throw new NotImplementedException();
        }
        public override void AddInventoryItem(byte index, Item invItem) {
            throw new NotImplementedException();
        }
        public override void AddItemText(ushort itemID, uint windowID, ushort length, string message) {
            throw new NotImplementedException();
        }
        public override void AddLocalChat(ChatLocal chatType, string message, Position pos, string nameFrom) {
            throw new NotImplementedException();
        }
        public override void AddLoginBytes(Player player, Map map) {
            netmsg.AddByte(0x0A);
            netmsg.AddU32(player.GetID());
            netmsg.AddByte(0x32);
            netmsg.AddByte(0x00);
            AddScreenMap(map, player);
            AddStats(player);
            UpdateWorldLight(map.GetWorldLightLevel());
            UpdateCreatureLight(player, player.SpellLightLevel);
            AddSkills(player);
            AddFullInventory(player);
        }
        public override void AddMOTD(string message, ushort messageNumber) {
            throw new NotImplementedException();
        }
        public override void AddRequestOutfit(Creature creature) {
            throw new NotImplementedException();
        }
        public override void AddScreenCreature(Creature creature, bool knowsCreature, Position pos, byte stackpos) {
            throw new NotImplementedException();
        }
        public override void AddScreenMap(Map map, Player player) {
            Position pos = player.CurrentPosition;
            netmsg.AddByte(0x64);
            netmsg.AddPosition(player.CurrentPosition);
            AddFullMap(pos, map, player);
        }
        public override void AddScreenMoveByOne(Direction direction, Position oldPos, Position newPos, Map map, Player player) {
            throw new NotImplementedException();
        }

        public override void AddStatusMessage(string msg) {
            throw new NotImplementedException();
        }
        public override void AddUserInfo(string message) {
            throw new NotImplementedException();
        }
        public override void AddUserList(List<string> names) {
            throw new NotImplementedException();
        }
        public override void Close() {
            netmsg.Close();
        }
        public override void RemoveInventoryItem(byte index) {
            throw new NotImplementedException();
        }
        public override void UpdateOpenContainer(byte localID, Container cont) {
            throw new NotImplementedException();
        }
        public override void UpdateWorldLight(byte light) {
            netmsg.AddByte(0x82);
            netmsg.AddByte(light);
            netmsg.AddByte(0xD7); //Light type, yellow = 0xD7
        }
        public override void UpdateCarryingItem(byte localID, UpdateCarryingType type, Item newItem) {
            throw new NotImplementedException();
        }
        public override void UpdateCreatureDirection(Creature creature, Direction direction, byte stackpos, Position position) {
            throw new NotImplementedException();
        }
        public override void UpdateCreatureOutfit(Creature creature) {
            throw new NotImplementedException();
        }
        public override void Reset() {
            netmsg.Reset();
        }
        public override void WriteToSocket() {
            netmsg.WriteToSocket();
        }

        public override void AddCharacterList(List<string> chars, string gameWorld, uint IP, ushort port, ushort premDays) {
            netmsg.AddByte(0x64);
            netmsg.AddByte((byte)chars.Count);
            for (int i = 0; i < chars.Count; i++) {
                netmsg.AddStringL(chars[i]);
                netmsg.AddStringL(gameWorld);
                netmsg.AddU32(IP);
                netmsg.AddU16(port);
            }

            netmsg.AddU16(premDays);
        }
    }
}
