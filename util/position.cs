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
    /// The Position class, used to hold x, y, and z coordinates.
    /// </summary>
    public class Position {
        /* These member variables are left public in order
         * to avoid cumbersome notation as these member variables
         * will be used frequently.
         */
        public ushort x;
        public ushort y;
        public byte z;

        /// <summary>
        /// Default position constructor, initializes position to (0, 0, 0).
        /// </summary>
        public Position() : this(0, 0, 0) {
        }

        /// <summary>
        /// Position constructor, initializes position to the specified values.
        /// </summary>
        /// <param name="xVal">x value</param>
        /// <param name="yVal">y value</param>
        /// <param name="zVal">z value</param>
        public Position(ushort xVal, ushort yVal, byte zVal) {
            x = xVal;
            y = yVal;
            z = zVal;
        }


        /// <summary>
        /// Returns a copy of this position.
        /// </summary>
        /// <returns>position copy.</returns>
        public Position Clone() {
            Position tmp = new Position();
            tmp.x = x;
            tmp.y = y;
            tmp.z = z;
            return tmp;
        }

        /// <summary>
        /// Determines whether two positions are equal.
        /// If the other position is null, they are not considered equal.
        /// </summary>
        /// <param name="two">The second position to compare</param>
        /// <returns>True if the positions are equal, false otherwise</returns>
        public override bool Equals(Object toCompare) {
            if (toCompare == null) {
                return false;
            }

            if (toCompare == this)
                return true;

            if (!(toCompare is Position))
                return false;

            Position two = (Position)toCompare;
            return (x == two.x && y == two.y && z == two.z);
        }

        /// <summary>
        /// Gets the hash code as specified by the x and y coordinates.
        /// </summary>
        /// <param name="x">The specified x coordiante</param>
        /// <param name="y">The specified y coordinate</param>
        /// <returns>An integer hash of the specified x and y coordinates</returns>
        public static int HashCode(ushort x, ushort y) {
            return (x << 16) | y;
        }

        /// <summary>
        /// Gets the object's hash code. Returns the position's unique
        /// hash code for its x and y coordinates. Note: Does not consider z. Be warned.
        /// </summary>
        /// <returns>
        /// Object's hash code.
        /// </returns>
        public override int GetHashCode() {
            return HashCode(x, y);
        }

        /// <summary>
        /// Gets to string representation of this class.
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString() {
            return "x: " + x + " y: " + y + " z: " + z;
        }



        /// <summary>
        /// Given an old position and a direction, this method returns
        /// a new position based on that direction.
        /// </summary>
        /// <param name="oldPos">The old position.</param>
        /// <param name="direction">The direction to move.</param>
        /// <returns>The new position.</returns>
        public static Position GetNewPosition(Position oldPos, Direction direction) {
            Position pos = oldPos.Clone();
            switch (direction) {
                case Direction.NORTH:
                    pos.y--;
                    break;
                case Direction.EAST:
                    pos.x++;
                    break;
                case Direction.SOUTH:
                    pos.y++;
                    break;
                case Direction.WEST:
                    pos.x--;
                    break;
                default:
                    Log.WriteLine("Unknown direction: " + direction);
                    break;
            }
            return pos;
        }
        /*/// <summary>
        /// Given a new position, this method returns a direction.
        /// Note: This position and the new position MUST be next
        /// to each other.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        public Direction GetNewDirection(Position newPosition) {
            int deltaX = x - newPosition.x;
            int deltaY = y - newPosition.y;
            Console.WriteLine("deltax: " + deltaX);
            Console.WriteLine("deltay: " + deltaY);
            if (deltaX == 1 && deltaY == 1) {
                return Direction.NORTHWEST;
            } else if (deltaX == 1 && deltaY == -1) {
                return Direction.SOUTHWEST;
            } else if (deltaX == -1 && deltaY == 1) {
                return Direction.NORTHEAST;
            } else if (deltaX == -1 && deltaY == -1) {
                return Direction.SOUTHEAST;
            } else if (deltaX == -1) {
                return Direction.EAST;
            } else if (deltaX == 1) {
                return Direction.WEST;
            } else if (deltaY == -1) {
                return Direction.SOUTH;
            } else if (deltaY == 1) {
                return Direction.NORTH;
            }
            return Direction.NONE;
        }*/
    }
}
