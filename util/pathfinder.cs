

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Cyclops {

    public class PathFinder {
        private Map gameMap;

        public PathFinder(Map gameMapRef) {
            gameMap = gameMapRef;
        }

        public bool GetPathTo(Creature creature, Position destPos,
            ref List<byte> listDir, Int32 maxSearchDist) {
            return GetPathTo(creature, destPos, listDir, 12, false);
        }
        public bool GetPathTo(Creature creature, Position destPos,
            List<byte> listDir, Int32 maxSearchDist, bool allowZChange) {
            return GetPathTo(creature, destPos, listDir, maxSearchDist, allowZChange, false);
        }

        public bool GetPathTo(Creature creature, Position destPos,
            List<byte> listDir, Int32 maxSearchDist, bool allowZChange, bool intendingToReachDes) {

            if (intendingToReachDes &&
                gameMap.TileContainsType(destPos, Constants.TYPE_BLOCKS_AUTO_WALK)) {
                    listDir.Clear();
                return false;
            }

            listDir.Clear();

            Position startPos = creature.CurrentPosition;
            Position endPos = destPos;
            if (endPos == null) {
                return false;
            }

            if (startPos.z != endPos.z)
                return false;

            AStarNodes nodes = new AStarNodes();
            AStarNode startNode = nodes.CreateOpenNode();

            startNode.x = startPos.x;
            startNode.y = startPos.y;

            startNode.g = 0;
            startNode.h = nodes.GetEstimatedDistance
                (startPos.x, startPos.y, endPos.x, endPos.y);

            startNode.f = startNode.g + startNode.h;
            startNode.parent = null;

            Position pos = new Position();
            pos.z = startPos.z;

            short[,] neighbourOrderList = 
            {
            {-1, 0},
            {0, 1},
            {1, 0},
            {0, -1},

               		//diagonal
	        {-1, -1},
	        {1, -1},
	        {1, 1},
	        {-1, 1},

            };

            //MapTile tile = null;
            AStarNode found = null;

            while (nodes.CountClosedNodes() < 100) {
                AStarNode n = nodes.GetBestNode();
                if (n == null) {
                    listDir.Clear();
                    return false; //No path found
                }

                if (n.x == endPos.x && n.y == endPos.y) {
                    found = n;
                    break;
                } else {
                    for (int i = 0; i < 8; i++) {
                        if (i > 3 && (allowZChange || intendingToReachDes)) {
                            continue;
                        }
                        //Console.WriteLine("neighbourhood["+i+", 1]" + neighbourOrderList[i, 0]);
                        pos.x = (ushort)(n.x + neighbourOrderList[i, 0]);
                        pos.y = (ushort)(n.y + neighbourOrderList[i, 1]);
                        int endPosX = endPos.x;
                        int endPosY = endPos.y;
                        int posX = pos.x;
                        int posY = pos.y;


                        bool outOfRange = false;
                        if (Math.Abs(endPosX - posX) > maxSearchDist ||
                            Math.Abs(endPosY - posY) > maxSearchDist) {
                            outOfRange = true;
                        }

                        if ((!outOfRange) &&
                       (!gameMap.TileContainsType(pos, Constants.TYPE_BLOCKS_AUTO_WALK))
                       || (destPos.x == pos.x && destPos.y == pos.y)) {
                            if (i > 3 && !destPos.Equals(pos)) {
                                nodes.CloseNode(n);
                                continue;
                            }

                            int cost = 0;
                            int extraCost = 0;
                            int newg = n.g + cost + extraCost;

                            AStarNode neighbourNode = nodes.GetNodeInList(pos.x, pos.y);
                            if (neighbourNode != null) {
                                if (neighbourNode.g <= newg) {
                                    continue;
                                }
                                nodes.OpenNode(neighbourNode);
                            } else {
                                neighbourNode = nodes.CreateOpenNode();
                                if (neighbourNode == null) {
                                    listDir.Clear();
                                    return false;
                                }
                            }

                            neighbourNode.x = pos.x;
                            neighbourNode.y = pos.y;
                            neighbourNode.parent = n;
                            neighbourNode.g = newg;
                            neighbourNode.h = (nodes.GetEstimatedDistance((int)neighbourNode.x,
                                (int)neighbourNode.y, (int)endPos.x, (int)endPos.y));
                            neighbourNode.f = neighbourNode.g + neighbourNode.h;
                        }
                    }
                    nodes.CloseNode(n);
                }
            }

            int prevX = endPos.x;
            int prevY = endPos.y;
            int dx, dy;

            while (found != null) {
                pos.x = (ushort)found.x;
                pos.y = (ushort)found.y;


                found = found.parent;

                dx = pos.x - prevX;
                dy = pos.y - prevY;

                prevX = pos.x;
                prevY = pos.y;

                if (dx == 1)
                    listDir.Insert(0, (byte)Direction.WEST);
                else if (dx == -1)
                    listDir.Insert(0, (byte)Direction.EAST);
                else if (dy == 1)
                    listDir.Insert(0, (byte)Direction.NORTH);
                else if (dy == -1)
                    listDir.Insert(0, (byte)Direction.SOUTH);
            }


            bool empty = true;
            if (listDir.Count == 0)
                empty = false;

            return (!empty);
        }
    }

    public class AStarNode {
        public Int32 x, y;
        public AStarNode parent;
        public Int32 f, g, h;
    }

    public class AStarNodes {
        public AStarNode[] nodes;
        BitArray openNodes = new BitArray(512);
        UInt32 curNode;

        public AStarNodes() {
            nodes = new AStarNode[512];
            for (int i = 0; i < 512; i++) {
                nodes[i] = new AStarNode();
            }

            curNode = 0;
            openNodes.SetAll(false);
        }

        public AStarNode CreateOpenNode() {
            if (curNode >= 512)
                return null;

            UInt32 retNode = curNode;
            curNode++;
            openNodes.Set((int)retNode, true);
            return nodes[retNode];
        }

        public AStarNode GetBestNode() {
            if (curNode == 0)
                return null;
            int bestNodeF = 100000;
            UInt32 bestNode = 0;
            bool found = false;
            for (UInt32 i = 0; i < curNode; i++) {
                if (nodes[i].f < bestNodeF &&
                    (openNodes.Get((int)i) == true)) {
                    found = true;
                    bestNodeF = nodes[i].f;
                    bestNode = i;
                }
            }
            if (found)
                return nodes[bestNode];

            return null;
        }

        public uint GetNodeIndex(AStarNode node) {
            for (uint i = 0; i < 512; i++) {
                if (nodes[i] == node)
                    return i;
            }
            return 520;
        }

        public void CloseNode(AStarNode node) {
            UInt32 pos = GetNodeIndex(node);
            if (pos >= 512) {
                Console.WriteLine("closeNodePosition failed.....");
                return;
            }

            openNodes[(int)pos] = false;
        }

        public void OpenNode(AStarNode node) {
            UInt32 pos = GetNodeIndex(node);
            if (pos >= 512) {
                Console.WriteLine("openNode pos failed");
                return;
            }
            openNodes[(int)pos] = false;
        }

        public UInt32 CountClosedNodes() {
            UInt32 counter = 0;
            for (UInt32 i = 0; i < curNode; i++) {
                if (openNodes[(int)i] == false)
                    counter++;
            }
            return counter;
        }

        public UInt32 CountOpenNodes() {
            UInt32 counter = 0;
            for (UInt32 i = 0; i < curNode; i++) {
                if (openNodes[(int)i] == true)
                    counter++;
            }
            return counter;
        }

        public bool IsInList(UInt32 x, UInt32 y) {
            for (UInt32 i = 0; i < curNode; i++) {
                if (nodes[i].x == x && nodes[i].y == y)
                    return true;
            }
            return false;
        }

        public AStarNode GetNodeInList(UInt32 x, UInt32 y) {
            for (UInt32 i = 0; i < curNode; i++) {
                if (nodes[i].x == x && nodes[i].y == y)
                    return nodes[i];
            }

            return null;
        }

        public Int32 GetMapWalkCost() {
            return 0;
        }

        public Int32 GetTileWalkCost(Tile tile) {
            return 0;
        }

        public int GetEstimatedDistance(int x, int y,
            int xGoal, int yGoal) {
            int hStraight = (int)(Math.Abs(x - xGoal) + Math.Abs(y - yGoal));

            return hStraight * 10;
        }
    }
}
