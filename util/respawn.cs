using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Cyclops {
    public class Respawn {

        private GameWorld world;

        public Respawn(GameWorld gameWorld) {
            world = gameWorld;
        }

        public ushort CenterX {
            get;
            set;
        }
        public ushort CenterY {
            get;
            set;
        }

        public byte CenterZ {
            get;
            set;
        }

        public int Radius {
            get;
            set;
        }

        public string MonsterName {
            get;
            set;
        }

        public int SpawnTime {
            get;
            set;
        }
        private static int x = 0;

        /// <summary>
        /// Todo: Finish code.
        /// </summary>
        /// <returns></returns>
        public bool CanSpawn() {
            if (!Monster.ExistsMonster(MonsterName)) {
                return false;
            }
            Position pos = new Position(CenterX, CenterY, CenterZ);
            Map map = world.GetGameMap();
            Tile tile = map.GetTile(pos);
            if (tile == null || tile.ContainsType(Constants.TYPE_BLOCKS_AUTO_WALK)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add creature to game map. Note: Caching is Used to speed up adding
        /// a creature.
        /// </summary>
        public void Spawn() {
            Monster monster = Monster.CreateMonster(MonsterName);
            world.AddCachedCreature(monster, new Position(CenterX, CenterY, CenterZ));
        }

        //TODO: Completely rework this method... it currently sucks
        //A LOT... i mean it!
        public void CheckForRespawn() {
            Position pos = new Position(CenterX, CenterY, CenterZ);
            Monster monster = Monster.CreateMonster(MonsterName);
            if (monster != null && world.GetGameMap().GetTile(pos) != null) {
                world.AppendAddMonster(monster, pos);
            }
            return;

            Map map = world.GetGameMap();
            ThingSet tSet = map.GetThingsInVicinity(pos);
            //Monster monster = Monster.CreateMonster(MonsterName);
            if (monster == null)
                return;

            if (map.GetTile(pos) != null &&
                !map.TileContainsType(pos, Constants.TYPE_BLOCKS_AUTO_WALK)) {
                    bool canRespawn = true;
                    foreach (Thing thing in tSet.GetThings()) {
                        if (thing is Player) { //TODO: FIX this crap
                            canRespawn = false;
                        }
                    }
                if (canRespawn)
                    world.SendAddMonster(monster, pos);
            } else {
                return;
            }

          //  if (map.GetTile(pos) != null &&
               // !map.TileContainsType(pos, Constants.TYPE_BLOCKS_AUTO_WALK)) {
                  // Console.WriteLine("x: " + x++);
                //world.SendAddMonster(monster, pos);
           // } 

            world.AddEventInCS(SpawnTime, CheckForRespawn);
        }

        /// <summary>
        /// TODO: Multiple spawns please.
        /// </summary>
        /// <param name="world"></param>
        public static void Load(GameWorld world) {
            string path = Config.GetDataPath() + "world/spawns.xml";
            XmlTextReader reader = new XmlTextReader(path);
            Respawn currentRespawn = null;
            while (reader.Read()) {
                switch (reader.NodeType) {
                    case XmlNodeType.Element:
                        while (reader.MoveToNextAttribute()) // Read attributes
                            {
                            if (reader.Name == "centerx") {
                                currentRespawn = new Respawn(world);
                                currentRespawn.CenterX = ushort.Parse(reader.Value);
                            } else if (reader.Name == "centery") {
                                currentRespawn.CenterY = ushort.Parse(reader.Value);
                            } else if (reader.Name == "centerz") {
                                currentRespawn.CenterZ = byte.Parse(reader.Value);
                            } else if (reader.Name == "radius") {
                                currentRespawn.Radius = int.Parse(reader.Value);
                            } else if (reader.Name == "name") {
                                currentRespawn.MonsterName = reader.Value;
                            } else if (reader.Name == "spawntime") {
                                if (currentRespawn.CanSpawn()) {
                                    currentRespawn.Spawn();
                                }
                                //currentRespawn.SpawnTime = 100 * int.Parse(reader.Value);
                            }
                        }
                        break;
                }
            }

            reader.Close();
        }
    }
}
