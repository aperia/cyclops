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

namespace Cyclops {
   public class Map {
        private const byte Z_LEVELS = 16;

        private Dictionary<int, Tile>[] cachedTiles =
        new Dictionary<int, Tile>[Z_LEVELS];

        private Dictionary<int, ushort[]>[] mainTiles =
            new Dictionary<int, ushort[]>[Z_LEVELS];

        /// <summary>
        /// Given the specified coordinates, this method
        /// returns a unique string based on those coordinates.
        /// </summary>
        /// <returns></returns>
        private static string Serialize(int x, int y, byte z) {
            string serial = x + "," + y;
            return serial;
        }

        /// <summary>
        /// Gets the z iterator relative to whom the player is visible
        /// for.
        /// </summary>
        /// <param name="startZ">The z to start at.</param>
        /// <param name="endZ">The z to end at.</param>
        /// <param name="zStep">Which direction to move the z iterator.</param>
        /// <param name="z">The z map coordinate.</param>
        private static void GetZIter(ref short startZ, ref short endZ,
            ref short zStep, byte z) {
            if (z > 7) { //Underground
                startZ = (byte) Math.Max(8, z - 2);
                endZ = Math.Min((short)(Constants.MAP_MAX_LAYERS - 1), (short)(z + 2));
                zStep = 1;
            } else { //Above ground
                startZ = (byte) Math.Max(7, z + 2);
                endZ = 0;
                zStep = -1;
            }
        }

        /// <summary>
        /// Constructor, used to initialize a Map object. Map is 
        /// loaded in the constructor.
        /// </summary>
        public Map(int sizex, int sizey) {
            for (int i = 0; i < cachedTiles.Length; i++) {
                cachedTiles[i] = new Dictionary<int, Tile>();
            }
            for (int i = 0; i < mainTiles.Length; i++) {
                mainTiles[i] = new Dictionary<int, ushort[]>();
            }
        }


        /// <summary>
        /// Load the map.
        /// </summary>
        public static Map Load() {
            FileHandler handler = new FileHandler();
            //string path = Config.GetDataPath() + "world/" + Config.GetMapName(); TODO:REMOV
			string path = Config.GetDataPath() + Config.
            BinaryReader bReader = new BinaryReader(File.Open(path, FileMode.Open));
            int sizex = bReader.ReadUInt16(); //Height
            int sizey = bReader.ReadUInt16(); //Width
            Map map = new Map(sizex, sizey);

            uint tileCount = bReader.ReadUInt32(); //Tile Count
            for (int i = 0; i < tileCount; i++) {
                ushort x = bReader.ReadUInt16(); //X position
                ushort y = bReader.ReadUInt16(); //Y position
                byte z = bReader.ReadByte(); //Z position
                ushort id = bReader.ReadUInt16(); //Tile ID
                byte itemCount = bReader.ReadByte();

                ushort[] mainTile = new ushort[itemCount + 1]; //Item count + ground
                mainTile[0] = id;
                //map.SetTile(x, y, z, new Tile(Item.CreateItem(id)));

                for (int j = 1; j <= itemCount; j++) {
                    ushort itemID = bReader.ReadUInt16();
                    mainTile[j] = itemID;
                }
                int hashCode = Position.HashCode(x, y);
                map.mainTiles[z].Add(hashCode, mainTile);
            }

            bReader.Close();
            return map;
        }

        /// <summary>
        /// Gets all the things in the vicinity (as specified in the parameters)
        /// and adds them to the ThingSet specified in the parameters.
        /// Note: general vicinity is specified as an 18x14 box w/ the Position
        /// in the center at x == 6 && y == 7.
        /// </summary>
        /// <param name="position">The position for which to get the things
        /// in Vicinity.</param>
        /// <param name="tSet">The ThingSet to add all the things gotten in 
        /// the vacanity.</param>
        /// <param name="leftXOffset">The left offset from the general Vicinity.</param>
        /// <param name="rightXOffset">The right offset from the general Vicinity.</param>
        /// <param name="leftYOffset">The left offset from the general Vicinity.</param>
        /// <param name="rightYOffset">The right offset from the gernal Vicinity</param>
        /// <param name="noZChange">True if only get things on the same z level or false
        /// in order to get things on all z levels defined in the general Vicinity.</param>
        public void GetThingsInVicinity(Position position, ThingSet tSet, int leftXOffset,
            int rightXOffset, int leftYOffset, int rightYOffset, bool noZChange) {
            short startZ = 0;
            short endZ = 0;
            short zStep = 1;
            if (noZChange) {
                startZ = endZ = position.z;
            } else {
                GetZIter(ref startZ, ref endZ, ref zStep, position.z);
            }
            //Original x: -9, +8, y: -7, +6
            for (int x = position.x - 9 - leftXOffset; x <= position.x + 9 + rightXOffset; x++) {
                for (int y = position.y - 7 - leftYOffset; y <= position.y + 6 + rightYOffset; y++) {
                    for (short z = startZ; z != endZ + zStep; z += zStep) {
                        short offset = (short)(position.z - z);
                        Tile mapTile = GetTile((ushort)(x + offset), (ushort)(y + offset), (byte)(z));
                        if (mapTile == null)
                            continue;

                        mapTile.GetThings(tSet);
                    }
                }
            }
        }

       /// <summary>
       /// Gets a set of things that are in the vacinity of a whipser.
       /// </summary>
       /// <param name="center">The center of the vacinity.</param>
        public void GetThingsInWhisperVicinity(Position center, ThingSet tSet) {
            GetThingsInVicinity(center, tSet, -8, -7, -6, -5, true);
        }
       
       /// <summary>
       /// Gets a set of things are are in the vacinity of a yell.
       /// </summary>
        /// <param name="center">The center of the vacinity.</param>
        /// <returns>A set of things in the yell vacinity.</returns>
       public void GetThingsInYellVacinity(Position center, ThingSet tSet) {
            GetThingsInVicinity(center, tSet, 8, 7, 6, 5, false);
        }


        /// <summary>
        /// Gets things in the vicinity.
        /// </summary>
        /// <param name="position">The position of the vicinity.</param>
        /// <param name="noZChange">True if only to get things on the same z level or false
        /// in order to get things on all z levels defined in the general Vicinity.</param>
        /// <returns>All the things in the Vicinity.</returns>
        public ThingSet GetThingsInVicinity(Position position, bool noZChange) {
            ThingSet tSet = new ThingSet();
            GetThingsInVicinity(position, tSet, 0, 0, 0, 0, noZChange);
            return tSet;
        }

        /// <summary>
        /// Gets things in the vicinity.
        /// </summary>
        /// <param name="position">The position of the vicinity.</param>
        /// <param name="tSet">The ThingSet to add to.</param>
        public void GetThingsInVicinity(Position position, ThingSet tSet) {
            GetThingsInVicinity(position, tSet, 0, 0, 0, 0, false);
        }

        /// <summary>
        /// Gets things in the vicinity.
        /// </summary>
        /// <param name="position">The position of the Vicinity.</param>
        /// <returns>All the things in the Vicinity.</returns>
        public ThingSet GetThingsInVicinity(Position position) {
            return GetThingsInVicinity(position, false);
        }

        /// <summary>
        /// Set the tile at the specified position.
        /// </summary>
        /// <param name="x">The x coordinate of the position.</param>
        /// <param name="y">The y coordinate of the position.</param>
        /// <param name="z">The z coordinate of the position.</param>
        /// <param name="tile">The tile to set.</param>
        public void SetTile(ushort x, ushort y, byte z, Tile tile) {
            int hash = Position.HashCode(x, y);
            cachedTiles[z].Add(hash, tile);
        }

        /// <summary>
        /// Set the tile at the specified position.
        /// </summary>
        /// <param name="pos">The position to set the tile at.</param>
        /// <param name="tile">The tile to set.</param>
        public void SetTile(Position pos, Tile tile) {
            SetTile(pos.x, pos.y, pos.z, tile);
        }

        /// <summary>
        /// Get the tile at the specified position.
        /// </summary>
        /// <param name="pos">Position to get the tile at.</param>
        /// <returns>The tile at the specified position.</returns>
        public Tile GetTile(Position pos) {
            return GetTile(pos.x, pos.y, pos.z);
        }

        /// <summary>
        /// Get the tile at the specified position or null if no
        /// such maptile exists.
        /// </summary>
        /// <param name="x">The x coordinate of the position.</param>
        /// <param name="y">The y coordinate of the position.</param>
        /// <param name="z">The z coordinate of the position.</param>
        /// <returns>The tile at the specified position.</returns>
        public Tile GetTile(int x, int y, byte z) {
            int hash = Position.HashCode((ushort)x, (ushort)y);
            Tile tile;
            //Check if tile is cached
            bool exists = cachedTiles[z].TryGetValue(hash, out tile);
            if (exists) {
                return tile;
            }

            ushort[] vals;
            //Tile isn't cached, try main
            exists = mainTiles[z].TryGetValue(hash, out vals);
            if (exists) {
                //Create tile and add to cached
                Item ground = Item.CreateItem(vals[0]);
                ground.CurrentPosition = new Position((ushort)x, (ushort)y, z);
                tile = new Tile(ground);
                for (int i = 1; i < vals.Length; i++) {
                    if (vals[i] == 0) {
                        Tracer.Println("Invalid ID at position x: " + x + " y: " + y + " z: " + z);
                    } else {
                        Item item = Item.CreateItem(vals[i]);
                        item.CurrentPosition = new Position((ushort)x, (ushort)y, z);
                        tile.AddThing(item);
                    }
                }

                //Cache it
                cachedTiles[z].Add(hash, tile);
                return tile;
            }

            return null;
        }

        /// <summary>
        /// Gets the top thing on the tile or null if there is no tile there.
        /// </summary>
        /// <param name="thing"></param>
        public Thing GetTopThing(Position position) {
            if (GetTile(position) == null) {
                return null;
            }
            return GetTile(position).GetTopThing();
        }

        /// <summary>
        /// Adds a thing with the specified position to the map.
        /// </summary>
        /// <param name="thing">The thing to add.</param>
        /// <param name="position">The position to add the thing to.</param>
        public void AddThing(Thing thing, Position position) {
            GetTile(position).AddThing(thing);
            thing.CurrentPosition = position;
        }

        /// <summary>
        /// Removes a thing from the specified position.
        /// </summary>
        /// <param name="thing">The thing to remove.</param>
        /// <param name="pos">The position to remove from.</param>
        public void RemoveThing(Thing thing, Position pos) {
            GetTile(pos).RemoveThing(thing);
            thing.CurrentPosition = null;
        }

        /// <summary>
        /// Moves the specified thing.
        /// </summary>
        /// <param name="thing">The thing to move.</param>
        /// <param name="oldPos">The thing's old position.</param>
        /// <param name="newPos">The thing's new position.</param>
        public void MoveThing(Thing thing, Position oldPos, Position newPos) {
            RemoveThing(thing, oldPos);
            AddThing(thing, newPos);
            thing.CurrentPosition = newPos;
        }


        /// <summary>
        /// Get the current world light level.
        /// </summary>
        /// <returns>world light level.</returns>
        public byte GetWorldLightLevel() {
            return 0x06; //TODO: Finish
        }

        /// <summary>
        /// Gets the stack position of a specified thing.
        /// Prevariant: That thing for which this method is called
        /// MUST be on the tile.
        /// </summary>
        /// <param name="thing">Thing to get the stack position for.</param>
        /// <param name="position">The position of the thign.</param>
        /// <returns>The thing's stack position.</returns>
        public byte GetStackPosition(Thing thing, Position position) {
            return GetTile(position).GetStackPosition(thing);
        }

       /// <summary>
       /// Gets whether the tile at the specified position contains 
       /// any thing that is of the specified type.
       /// </summary>
       /// <param name="pos">The position to check.</param>
       /// <param name="type">The type to check.</param>
       /// <returns>True if tile contains such a type, false otherwise.</returns>
        public bool TileContainsType(Position pos, uint type) {
            Tile tile = GetTile(pos);
            if (tile == null) {
                return false;
            }

            return tile.ContainsType(type);
        }
        
       /// <summary>
       /// Gets a position for which a creature could be added.
       /// Returns null if all the spaces around this position are taken.
       /// </summary>
       /// <param name="curPos">The position to add the creature.</param>
       /// <param name="creatureFor">The creature to add.</param>
       /// <returns>A free space or null if all spaces are taken.</returns>
        public Position GetFreePosition(Position curPos, Creature creatureFor) {
            Position tempPos = curPos.Clone();

            if (!TileContainsType(curPos, Constants.TYPE_BLOCKS_AUTO_WALK)) {
                return tempPos;
            }


            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    tempPos.x = (ushort)(curPos.x + i);
                    tempPos.y = (ushort)(curPos.y + j);
                    if (!TileContainsType(tempPos, Constants.TYPE_BLOCKS_AUTO_WALK)) {
                        return tempPos;
                    }
                }
            }

            return null;
        }
   }
}
