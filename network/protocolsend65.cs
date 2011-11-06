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
using Cyclops;

namespace Cyclops {
    public enum UpdateCarryingType {
        INVENTORY = 0,
        CONTAINER_ONE = 0x40,
        CONTAINER_TWO = 0x41
    }

    /// <summary>
    ///  This is the class that handles all of the protocol sending
    ///  related information. It must encapsulate protocol sending
    ///  and therefore any protocol sending related information 
    ///  should only be stored here.
    /// </summary>
    public class ProtocolSend65 : ProtocolSend {
        /// <summary>
        /// The type of update to do when
        /// updating a thing on the client's screen.
        /// </summary>
        private enum UpdateThing {
            REMOVE = 0,
            ADD = 1,
            UPDATE = 2
        }

        /// <summary>
        /// Creature's value to update when updating the creature.
        /// </summary>
        private enum UpdateCreature {
            HEALTH_STATUS = 1,
            LIGHT = 2,
            OUTFIT = 3,
        }

        List<Tile> potentialAddTiles = new List<Tile>();

        /// <summary>
        /// Adds the headers for updating a creature.
        /// </summary>
        /// <param name="c">The creature to update.</param>
        /// <param name="type">The type of update being done.</param>
        private void UpdateCreatureHeaders(Creature creature, UpdateCreature type) {
            netmsg.AddU16(0x32); //Update header
            netmsg.AddU32(creature.GetID());
            netmsg.AddByte((byte)type);
        }

        /// <summary>
        /// Update a thing header.
        /// </summary>
        /// <param name="pos">The position of the thing.</param>
        /// <param name="type">The type of update being done.</param>
        /// <param name="stackpos">The stackpos of the thing.</param>
        private void UpdateThingHeaders(Position pos, UpdateThing type, byte stackpos) {
            netmsg.AddU16(0x19);
            netmsg.AddPosition(pos);
            netmsg.AddByte((byte)type); //sub-header, 0 = remove, 1 = add, 2 = update
            netmsg.AddByte(stackpos);
        }


        private void AddItemRaw(Item item, bool carrying) {
            AddItemRaw(item, carrying, false, false);
        }

        private void AddOutfit(Creature creature) {
            if (creature.Invisible) {
                netmsg.AddU32(0x00);
            } else if (creature.Polymorph != 0) {
                netmsg.AddByte(creature.PolymorphCharType);
                netmsg.AddU16(creature.Polymorph);
                netmsg.AddByte(0x00);
            } else {
                netmsg.AddByte(creature.CharType);
                netmsg.AddByte(creature.OutfitUpper);
                netmsg.AddByte(creature.OutfitMiddle);
                netmsg.AddByte(creature.OutfitLower);
            }
        }

        private void AddItemRaw(Item item, bool carrying, bool defaultByte,
            bool screenItem) {
            netmsg.AddU16(item.ItemID);
            if (item.IsOfType(Constants.TYPE_LIGHT_ITEMS) && !carrying) {
                netmsg.AddByte(0x06); //Item light level
            } else if (item.IsOfType(Constants.TYPE_STACKABLE)) {
                netmsg.AddByte(item.Count); //Item count
            } else if (item.IsOfType(Constants.TYPE_FLUID_CONTAINER)) {
                netmsg.AddByte((byte)item.FluidType); //Fluid type
            } else if (defaultByte) { //Required
                netmsg.AddByte(0x00); //No type
            }

            if (screenItem) {
                netmsg.AddByte(0x00);
                netmsg.AddU16(0x00);
            }
        }

        private void AddChat(byte type, Position pos, string sender, string msg) {
            netmsg.AddU16(0x65);
            if (pos == null) {
                netmsg.AddPosition(new Position(0, 0, 0));
            } else {
                netmsg.AddPosition(pos);
            }

            netmsg.AddByte(type);

            if (sender != null) {
                netmsg.AddString(sender);
                netmsg.AddByte(0x09); //new line char
            }
            netmsg.AddStringZ(msg);
        }

        /// <summary>
        /// Adds the header information for direction.
        /// </summary>
        /// <param name="direction">The new direction.</param>
        /// <param name="creatureID">The creature's unique ID.</param>
        private void AddCreatureDirection(Direction direction, uint creatureID) {
            netmsg.AddByte(0xFA); //Direction header
            netmsg.AddByte((byte)direction);
            netmsg.AddU32(creatureID);
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
        /// Adds the map information as specified by the parameters.
        /// </summary>
        /// <param name="pos">Center of the map position.</param>
        /// <param name="width">Width of the map.</param>
        /// <param name="height">Height of the map.</param>
        /// <param name="map">A reference to the map.</param>
        /// <param name="player">The player for whom the 
        /// map information is sent.</param>
        private void AddMapInfo(Position pos, int width, int height, Map map, Player player) {
            potentialAddTiles.Clear();
            short startZ = 0, endZ = 0, zStep = 0;
            GetZIter(ref startZ, ref endZ, ref zStep, pos.z);
            for (short iterZ = startZ; iterZ != endZ + zStep; iterZ += zStep) {
                Position posTmp = new Position(pos.x, pos.y, (byte)iterZ);
                AddFloorInfo(posTmp, width, height, (short)(pos.z - iterZ), map, player);
            }

            //End sending map
            netmsg.SkipBytes(-1);
            netmsg.AddByte(0xFE);
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
             short offset, Map map, Player player) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Tile tile =  map.GetTile(x + pos.x + offset, y + pos.y + offset, pos.z);
                    potentialAddTiles.Add(tile);
                    if (tile != null) {
                        foreach (Tile potentialTile in potentialAddTiles) {
                            AddMapTile(potentialTile, player);
                        }
                        potentialAddTiles.Clear();
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
            netmsg.AddByte(0xFF); //End of maptile byte
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
        /// Protocol constructor.
        /// </summary>
        /// <param name="s"></param>
        public ProtocolSend65(Socket socket) {
            netmsg = new NetworkMessage(socket);
        }

        /// <summary>
        /// Reset the protocol.
        /// </summary>
        public override void Reset() {
            netmsg.Reset();
        }

        /// <summary>
        /// Write to the protocol's socket.
        /// </summary>
        public override void WriteToSocket() {
            netmsg.WriteToSocket();
        }

        /// <summary>
        /// Close the protocol stream.
        /// </summary>
        public override void Close() {
            netmsg.Close();
        }

        /// <summary>
        /// Remove a thing from the screen.
        /// </summary>
        /// <param name="pos">Thing's position.</param>
        /// <param name="stackpos">Thing's stack position.</param>
        public override void RemoveThing(Position pos, byte stackpos) {
            UpdateThingHeaders(pos, UpdateThing.REMOVE, stackpos);
            netmsg.AddU16(0x00);
            netmsg.AddU32(0x00);
            //TODO: Possibly remove creature light, will need to test
        }

        
        //todo: remove
        public void removepartial(Position pos, Item item, byte stackpos) {

            UpdateThingHeaders(pos, UpdateThing.REMOVE, stackpos);
            netmsg.AddU16(0x00);
            netmsg.AddU32(0x00);
        }


        /// <summary>
        /// Add a screen creature (when a maptile is sent) to the player's client.
        /// </summary>
        /// <param name="creature">The creature to add</param>
        /// <param name="knowsCreature">Whether the player knows the creature</param>
        /// <param name="pos">The position of the creature</param>
        /// <param name="stackpos">The stack position of the creature</param>
        public override void AddScreenCreature(Creature creature, bool knowsCreature, Position pos,
              byte stackpos) {
            UpdateThingHeaders(pos, UpdateThing.ADD, stackpos);
            AddTileCreature(creature, knowsCreature);
        }

        public override void UpdateItem(Position pos, Item item, byte stackpos) {
            UpdateThingHeaders(pos, UpdateThing.UPDATE, stackpos);
            AddItemRaw(item, false, true, true);
        }

        /// <summary>
        /// Adds an item to the player's client on the map.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="stackpos">The item's stack position.</param>
        public override void AddItem(Position pos, Item item, byte stackpos) {
            UpdateThingHeaders(pos, UpdateThing.ADD, stackpos);
            AddItemRaw(item, false, true, true);
        }

        /// <summary>
        /// Add a creature to the player's client on the map.
        /// </summary>
        /// <param name="creature">The creature to add.</param>
        /// <param name="knowsCreature">Whether the player knows the creature.</param>
        public override void AddTileCreature(Creature creature, bool knowsCreature) {
            netmsg.AddByte(0xFB);
            if (knowsCreature) {
                netmsg.AddU32(creature.GetID());
            } else {
                netmsg.AddU32(0);
            }

            netmsg.AddU32(creature.GetID());
            netmsg.AddStringZ(creature.Name, 30);
            netmsg.AddByte((byte)creature.CurrentHealthStatus);
            netmsg.AddByte((byte)creature.CurrentDirection);
            AddOutfit(creature);
            netmsg.AddByte(creature.GetLightLevel());
        }

        /// <summary>
        /// Adds a status message to the player's client.
        /// </summary>
        /// <param name="msg">The status message to add.</param>
        public override void AddStatusMessage(string msg) {
            netmsg.AddU16(0x68); //Status message header
            netmsg.AddStringZ(msg);
        }

        /// <summary>
        /// Updates a player's world light. 
        /// </summary>
        /// <param name="light">The light level to update.</param>
        public override void UpdateWorldLight(byte light) {
            netmsg.AddU16(0x28); //World light header
            netmsg.AddByte(light);
        }

        /// <summary>
        /// Updates a creature's health status.
        /// </summary>
        /// <param name="creature">The creature's health status to update.</param>
        public override void UpdateCreatureHealth(Creature creature) {
            UpdateCreatureHeaders(creature, UpdateCreature.HEALTH_STATUS);
            netmsg.AddByte((byte)creature.CurrentHealthStatus);
        }

        /// <summary>
        /// Updates a creature's light level.
        /// </summary>
        /// <param name="creature">The creature for whom the 
        /// light is being updated.</param>
        /// <param name="light">The light level to add.</param>
        public override void UpdateCreatureLight(Creature creature, byte light) {
            UpdateCreatureHeaders(creature, UpdateCreature.LIGHT);
            netmsg.AddByte(light);
        }

        /// <summary>
        /// Updates a creature's outfit.
        /// </summary>
        /// <param name="creature">The creature for whom 
        /// the outfit is being updated</param>
        public override void UpdateCreatureOutfit(Creature creature) {
            UpdateCreatureHeaders(creature, UpdateCreature.OUTFIT);
            AddOutfit(creature);
        }


        /// <summary>
        /// Add a player's full inventory.
        /// </summary>
        /// <param name="player">The player whose inventory is being updated.</param>
        public override void AddFullInventory(Player player) {
            //TODO: List each individually
            for (byte i = 1; i < Constants.INV_MAX; i++) {
                AddInventoryItem(i, player.GetInventoryItem(i));
            }
        }

        /// <summary>
        /// Add a item the player is carrying to the player's client.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public override void AddCarryingItem(Item item) {
            AddItemRaw(item, true);
        }

        /// <summary>
        /// Add a ground item to the player's client.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public override void AddGroundItem(Item item) {
            AddItemRaw(item, false);
        }

        /// <summary>
        /// Adds the login bytes to the player's client.
        /// </summary>
        /// <param name="player">The player for whom to add the login bytes.</param>
        /// <param name="map">A reference to the map.</param>
        public override void AddLoginBytes(Player player, Map map) {
            AddMOTD(Config.GetMOTD(), Config.GetMessageNumber());
            netmsg.AddU16(0x01); //Login header
            netmsg.AddU32(player.GetID()); //ID sent to the client
            AddScreenMap(map, player);
            AddEffect(MagicEffect.BLUEBALL, player.CurrentPosition);
            AddFullInventory(player);
            AddStats(player);
            AddStatusMessage(Config.GetWelcomeMessage());
            UpdateWorldLight(map.GetWorldLightLevel());
            UpdateCreatureLight(player, player.GetLightLevel());
        }

        
        /// <summary>
        /// Adds a "Sorry" messagebox to the player's client.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public override void AddSorryBox(string message) {
            netmsg.AddU16(0x02); //Sorry message box header
            netmsg.AddStringZ(message);
        }

        /// <summary>
        /// Adds a "For Your Information" messagebox to the player's client.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public override void AddForYourInformationBox(string message) {
            netmsg.AddU16(0x04); //For your information header
            netmsg.AddStringZ(message);
        }

        /// <summary>
        /// Sends the screen for requesting an outfit to the player's client.
        /// </summary>
        /// <param name="creature">Creature's outfit.</param>
        public override void AddRequestOutfit(Creature creature) {
            netmsg.AddU16(0x03);
            netmsg.AddStringZ(creature.Name, 30);

            netmsg.AddByte(00);
            netmsg.AddByte(creature.OutfitUpper);
            netmsg.AddByte(creature.OutfitMiddle);
            netmsg.AddByte(creature.OutfitLower);
            //netmsg.AddU32(0x10101010);
            //netmsg.AddByte(creature.OutfitUpper);
            //netmsg.AddByte(0x00);
            //netmsg.AddByte(creature.OutfitMiddle);
            //netmsg.AddByte(creature.OutfitLower);
            /*
            netmsg.AddByte(0x00);
            netmsg.AddByte(creature.OutfitUpper);
            netmsg.AddByte(creature.OutfitMiddle);
            netmsg.AddByte(creature.OutfitLower);*/
        }
        
        /// <summary>
        /// Adds a message of the day to the player's client.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <param name="messageNumber">The message number.</param>
        public override void AddMOTD(string message, ushort messageNumber) {
            netmsg.AddU16(0x05); //MOTD header
            netmsg.AddU16(messageNumber); //MOTD number
            netmsg.AddByte(0x0A); //New line char, to denote where msg starts
            netmsg.AddStringZ(message);
        }

        /// <summary>
        /// Adds the entire screen map.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="player"></param>
        public override void AddScreenMap(Map map, Player player) {
            Position pos = player.CurrentPosition;
            netmsg.AddU16(0x0A); //Add screen map header
            netmsg.AddPosition(pos);
            AddFullMap(pos, map, player);
            netmsg.AddByte(0x00); //End of 0x0A header
            //netmsg.AddByte(0x00);
            //netmsg.AddByte(0x03);
        }

        /// <summary>
        /// Sends a message to move a player's client by one tile.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        /// <param name="oldPos">Player's old position.</param>
        /// <param name="newPos">Player's new position.</param>
        /// <param name="map">A reference to the map.</param>
        /// <param name="player">The player for whom the 
        /// map information is being sent.</param>
        public override void AddScreenMoveByOne(Direction direction, Position oldPos,
            Position newPos, Map map, Player player) {
            ushort header = (ushort)(direction + 0x0B); //0x0B, 0x0C, 0x0D, 0x0E
            netmsg.AddU16(header);
            switch (direction) {
                case Direction.NORTH:
                    AddMapInfo(oldPos.x - 8, newPos.y - 6, newPos.z, 18,
                        1, map, player);
                    break;

                case Direction.EAST:
                    AddMapInfo(newPos.x + 9, newPos.y - 6, newPos.z, 1, 
                        14, map, player);
                    break;

                case Direction.SOUTH:
                    AddMapInfo(oldPos.x - 8, newPos.y + 7, newPos.z, 18,
                        1, map, player);
                    break;

                case Direction.WEST:
                    AddMapInfo(newPos.x - 8, newPos.y - 6, newPos.z, 1,
                        14, map, player);
                    break;
                default:
                    throw new Exception("Invalid direction sent in AddScreenMoveByOne()");
            }
            netmsg.AddByte(0x00); //End of move
        }

        /// <summary>
        /// Adds a container close to the player's client.
        /// </summary>
        /// <param name="localID">The local ID of the container.</param>
        public override void AddContainerClose(byte localID) {
            netmsg.AddU16(0x12); //Close container header
            netmsg.AddByte(localID);
        }

        /// <summary>
        /// Adds a container open to the player's client.
        /// </summary>
        /// <param name="localID">The local ID of the container.</param>
        /// <param name="cont">A reference to the container.</param>
        public override void AddContainerOpen(byte localID, Container cont) {
            netmsg.AddU16(0x13); //Open container header
            netmsg.AddByte(localID);
            netmsg.AddU16(cont.ItemID);
            foreach (Item item in cont.GetItems()) {
                AddCarryingItem(item);
            }
            netmsg.AddU16(0xFFFF);
        }

        /// <summary>
        /// Updates an open container.
        /// </summary>
        /// <param name="localID">The local ID of the container.</param>
        /// <param name="cont">A reference to the container.</param>
        public override void UpdateOpenContainer(byte localID, Container cont) {
            AddContainerOpen(localID, cont);
        }

        /// <summary>
        /// Adds an inventory item to the player's client.
        /// </summary>
        /// <param name="index">Inventory index to add.</param>
        /// <param name="invItem">Inventory item to add.</param>
        public override void AddInventoryItem(byte index, Item invItem) {
            if (invItem == null)
                return;

            netmsg.AddU16(0x14); //Inventory item header
            netmsg.AddByte(index);
            AddItemRaw(invItem, true, true, false);
        }

        /// <summary>
        /// Removes an Inventory item as specified by the index.
        /// </summary>
        /// <param name="index">Inventory index of the item.</param>
        public override void RemoveInventoryItem(byte index) {
            netmsg.AddU16(0x15); //Remove inventory item header
            netmsg.AddByte(index);
            netmsg.AddByte(0x00);
        }

        /// <summary>
        /// Update a carying item.
        /// </summary>
        /// <param name="localID">The local ID of the item.</param>
        /// <param name="type">The type of update being done.</param>
        /// <param name="newItem"></param>
        public override void UpdateCarryingItem(byte localID, UpdateCarryingType type,
            Item newItem) {
            netmsg.AddU16(0x16); //Add header
            netmsg.AddByte((byte)type);
            netmsg.AddByte(localID);
            AddItemRaw(newItem, true, true, false);
        }
        
        /// <summary>
        /// Adds a magic effect to the player's client.
        /// </summary>
        /// <param name="effect">Effect to add.</param>
        /// <param name="pos">Position to add the effect.</param>
        public override void AddEffect(MagicEffect effect, Position pos) {
            netmsg.AddU16(0x1A); //Magic effect header
            netmsg.AddPosition(pos);
            netmsg.AddByte((byte)effect);
        }


        /// <summary>
        /// Adds a magic shoot effect.
        /// </summary>
        /// <param name="effect">The effect to add.</param>
        /// <param name="origin">The origin of the shoot effect.</param>
        /// <param name="dest">The destination of the shoot effect.</param>
        public override void AddShootEffect(byte effect, Position origin, Position dest) {
            netmsg.AddU16(0x1B); //Shoot effect header
            netmsg.AddPosition(origin);
            netmsg.AddPosition(dest);
            netmsg.AddByte(effect);
        }

        /// <summary>
        /// Adds an item's text to the player's client.
        /// </summary>
        /// <param name="itemID">The item ID.</param>
        /// <param name="windowID">The local ID of the window.</param>
        /// <param name="length">Length of the message.</param>
        /// <param name="message">The message itself.</param>
        public override void AddItemText(ushort itemID, uint windowID, ushort length, string message) {
            netmsg.AddU16(0x23); //Text item header

            netmsg.AddU32(windowID); //windowTextID

            netmsg.AddU16(itemID); //Note: enables "edit" if item type can be edited

            netmsg.AddU16(length); //Length

            netmsg.AddStringZ(message);
        }

        /// <summary>
        /// Add house text to player's client.
        /// </summary>
        /// <param name="windowID">Local ID of the window.</param>
        /// <param name="length">Max length of the message.</param>
        /// <param name="message">Message to add.</param>
        public override void AddHouseText(uint windowID, ushort length, string message) {
            netmsg.AddU16(0x24);
            netmsg.AddByte(0x00); //Type (house guests, house co-owners etc.)
            netmsg.AddU32(windowID);
            netmsg.AddU16(length);
            netmsg.AddStringZ(message);
        }

        /// <summary>
        /// Add player stats to player's client.
        /// </summary>
        /// <param name="player">Player whose stats to add.</param>
        public override void AddStats(Player player) {
            netmsg.AddU16(0x3C); //Stats header
            netmsg.AddU16(player.CurrentHP); //hp
            netmsg.AddU16((ushort)player.GetCurrentCap()); //cap
            netmsg.AddU32(player.Experience); //xp
            netmsg.AddByte(player.Level); //lvl
            netmsg.AddU16(player.CurrentMana); //mana
            netmsg.AddByte(player.MagicLevel); //magic level
            netmsg.AddU16(player.GetAmmo()); //Ammo
        }

        /// <summary>
        /// Add player skills to player's client.
        /// </summary>
        /// <param name="player">The player to add.</param>
        public override void AddSkills(Player player) {
            netmsg.AddU16(0x3D); //Skills header
            netmsg.AddByte(player.GetSkill(Constants.SKILL_SWORD));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_CLUB));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_AXE));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_DISTANCE));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_SHIELDING));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_FIST));
            netmsg.AddByte(player.GetSkill(Constants.SKILL_FISHING));
        }

        /// <summary>
        /// Adds the protocol information for moving a creature.
        /// </summary>
        /// <param name="direction">The new direction of the creature.</param>
        /// <param name="creature">The creature to move.</param>
        /// <param name="oldPos">Creature's old position.</param>
        /// <param name="newPos">Creature's new position.</param>
        /// <param name="oldStackpos">Creature's old stack position.</param>
        /// <param name="newStackpos">Creature's new stack position.</param>
        public override void AddCreatureMove(Direction direction, Creature creature,
            Position oldPos, Position newPos, byte oldStackpos, byte newStackpos) {
            RemoveThing(oldPos, oldStackpos);

            UpdateThingHeaders(newPos, UpdateThing.ADD, newStackpos);

            AddCreatureDirection(direction, creature.GetID());
        }

        /// <summary>
        /// Update the creature's direction.
        /// </summary>
        /// <param name="creature">The creature whose direction to update.</param>
        /// <param name="direction">The new direction.</param>
        /// <param name="stackpos">The creature's stack position.</param>
        /// <param name="position">The creature's position.</param>
        public override void UpdateCreatureDirection(Creature creature,
            Direction direction, byte stackpos, Position position) {
            
            UpdateThingHeaders(position, UpdateThing.UPDATE, stackpos);
            AddCreatureDirection(direction, creature.GetID());
        }

        /// <summary>
        /// Sends a local chat event to the game client.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content of chat.</param>
        /// <param name="pos">Position of chat.</param>
        /// <param name="nameFrom">Name of creature sending the chat.</param>
        public override void AddLocalChat(ChatLocal chatType, string message, Position pos,
            string nameFrom) {
            AddChat((byte)chatType, pos, nameFrom, message);
        }

        /// <summary>
        /// Sends a local chat event to the game client.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content of chat.</param>
        /// <param name="pos">Position of chat.</param>
        /// <param name="nameFrom">Name of creature sending the chat.</param>
        public override void AddGlobalChat(ChatGlobal chatType, string message,
            string nameFrom) {
            AddChat((byte)chatType, null, nameFrom, message);
        }

        /// <summary>
        /// Sends a local chat event to the game client.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content of chat.</param>
        /// <param name="pos">Position of chat.</param>
        /// <param name="nameFrom">Name of creature sending the chat.</param>
        public override void AddAnonymousChat(ChatAnonymous chatType, string message) {
            AddChat((byte)chatType, null, null, message);
        }


        /// <summary>
        /// Adds online user list to player's client.
        /// </summary>
        /// <param name="names">Names to add.</param>
        public override void AddUserList(List<string> names) {
            netmsg.AddU16(0x66); //User list header
            netmsg.AddU16(0x1010); //# of bytes to allocate for text
            foreach (string name in names) {
                netmsg.AddString(name);
                netmsg.AddString("\n");
            }
            netmsg.AddStringZ("");
        }

        /// <summary>
        /// Adds the "User Info" messagebox to player's client.
        /// </summary>
        /// <param name="message">Message to add.</param>
        public override void AddUserInfo(string message) {
            netmsg.AddU16(0x67); //User information header
            netmsg.AddU16(0x1010); //# of bytes to allocate for text
            netmsg.AddStringZ(message); //Null-terminated string
        }

        /// <summary>
        /// Adds bounce back, all bytes sent to the client will be mirrored
        /// back to the server.
        /// </summary>
        public override void AddBounceBack() {
            netmsg.AddU16(0xC8);
        }

        public void buggabugga(int id, Position pos) {
            netmsg.Reset();
            UpdateCarryingItem(0, UpdateCarryingType.CONTAINER_ONE, null);
        }

        public override void AddCharacterList(List<string> chars, string gameWorld,
            uint IP, ushort port, ushort premDays) {
            netmsg.AddByte(0x64);
            netmsg.AddByte((byte)chars.Count);
            for (int i = 0; i < chars.Count; i++) {
                netmsg.AddStringL(chars[i]);
                netmsg.AddStringL(gameWorld);
                netmsg.AddU32(IP);
                netmsg.AddU16(port);
            }
        }
    }
}
