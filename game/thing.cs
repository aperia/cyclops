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
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace Cyclops {
    /// <summary>
    /// Abastract thing class, things such as creatures and items
    /// inherit from this class.
    /// </summary>
    public abstract class Thing : IComparable<Thing> {
        /// <summary>
        /// Gets the z iterator relative to this thing.
        /// </summary>
        /// <param name="startZ">The z to start at.</param>
        /// <param name="endZ">The z to end at.</param>
        /// <param name="zStep">Which direction to move the z iterator.</param>
        /// <param name="z">Player's z position.</param>
        private static void GetZIter(ref short startZ, ref short endZ,
            ref short zStep, byte z) {
            if (z > 7) {
                startZ = (byte)(z - 2);
                endZ = Math.Min((short)(Constants.MAP_MAX_LAYERS), (short)(z + 2));
                zStep = 1;
            } else {
                startZ = 7;
                endZ = 0;
                zStep = -1;
            }

        }

        /// <summary>
        /// Determines whether this thing can see the specified
        /// z level.
        /// </summary>
        /// <param name="z">The specified z level to check.</param>
        /// <returns>True if this thing can see the specified z level, 
        /// false if it can't</returns>
        private bool CanSeeZ(byte z) {
            if (CurrentPosition == null) {
                return false;
            }

            short startZ = 0;
            short endZ = 0;
            short zStep = 0;
            GetZIter(ref startZ, ref endZ, ref zStep, this.CurrentPosition.z);
            for (short zIter = startZ; zIter != endZ; zIter += zStep) {
                if (z == zIter)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this thing can see the specified position.
        /// Note: the z value for the specified position is assumed to be 
        /// the same as the z value of the thing's position.
        /// </summary>
        /// <param name="x">The x value of the specified position.</param>
        /// <param name="y">The y value of the specified position.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        protected bool CanSee(ushort x, ushort y) {
            return CanSee(x, y, 0, 0, 0, 0);
        }

        /// <summary>
        /// Determines whether this thing can see the specified position.
        /// Note: the z value for the specified position is assumed to be 
        /// the same as the z value of the thing's position.
        /// </summary>
        /// <param name="x">The x value of the specified position.</param>
        /// <param name="y">The y value of the specified position.</param>
        /// <param name="offsetDownY">Offset down of screen to use.</param>
        /// <param name="offsetLeftX">Offset left of screen to use.</param>
        /// <param name="offsetRightX">Offset right of screen to use.</param>
        /// <param name="offsetUpY">Offset up of screen to use.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        protected bool CanSee(ushort x, ushort y, int offsetLeftX, int
            offsetRightX, int offsetUpY, int offsetDownY) {
            int leftX = CurrentPosition.x - 8 - offsetLeftX;
            int rightX = CurrentPosition.x + 9 + offsetRightX;
            int upY = CurrentPosition.y - 6 - offsetUpY;
            int downY = CurrentPosition.y + 7 + offsetDownY;
            return ((x >= leftX) && (x <= rightX) && (y >= upY) && (y <= downY));
        }

        /// <summary>
        /// Initializes all the things needed by this class when
        /// a child class is constructed.
        /// </summary>
        public Thing() {
        }

        /// <summary>
        /// Checks whether the position is next to this thing's position.
        /// </summary>
        /// <param name="posToCheck">The position to check.</param>
        /// <returns>True if the position is next to, false if it is not.</returns>
        public bool IsNextTo(Position posToCheck) {
            //Position from creature to checking position
            ushort deltaX = (ushort)Math.Abs(this.CurrentPosition.x - posToCheck.x);
            ushort deltaY = (ushort)Math.Abs(this.CurrentPosition.y - posToCheck.y);
            byte deltaZ = (byte)Math.Abs(this.CurrentPosition.z - posToCheck.z);

            if (deltaX > 1 || deltaY > 1 || deltaZ > 0) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether this thing can see the specified position.
        /// </summary>
        /// <param name="pos">The specified position to check.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        public bool CanSee(Position pos) {
            return CanSee(pos.x, pos.y, pos.z);
        }

        /// <summary>
        /// Determines whether this thing can see the specified position only
        /// on the same floor.
        /// </summary>
        /// <param name="pos">The position to check</param>
        /// <param name="sameZ"></param>
        /// <returns></returns>
        public bool CanSeeSameZ(Position pos) {
            if (CurrentPosition == null)
                return false;

            if (CurrentPosition.z != pos.z) {
                return false;
            }

            return CanSee(pos);
        }


        /// <summary>
        /// Sends the protocol data for adding itself to the ground.
        /// </summary>
        /// <param name="proto">A reference to the protocol.</param>
        /// <param name="player">The player for whom to add this to.</param>
        /// <param name="stackPos">The stack position of this thing.</param>
        public abstract void AddThisToGround(ProtocolSend proto,
            Player player, Position pos, byte stackPos);

        /*
        /// <summary>
        /// Determines whether the the thing can see something
        /// on its actual screen. Note: Tibia 6.4 works weird in the
        /// sense that creatures aren't shown even if they are on the map
        /// if they are offscreen.
        /// </summary>
        /// <param name="x">The x value of the specified position.</param>
        /// <param name="y">The y value of the specified position.</param>
        /// <param name="z">The z value of the specified position.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        public bool CanSeeScreen(ushort x, ushort y, byte z) {
            if (!CanSeeZ(z))
                return false;

            if (this.CurrentPosition.z > z) {
                int offset = this.CurrentPosition.z - z;
                return CanSee((ushort)(x - offset),
                    (ushort)(y - offset), 2, -2, 2, -2);
            } else if (this.CurrentPosition.z < z) {
                int offset = z - this.CurrentPosition.z;
                return CanSee(
                    (ushort)(x + offset), (ushort)(y + offset), 2, -2, 2, -2);
            }
            return CanSee(x, y, 2, -2, 2, -2);
        }

        /// <summary>
        /// Determines whether the the thing can see something
        /// on its actual screen. Note: Tibia 6.4 works weird in the
        /// sense that creatures aren't shown even if they are on the map
        /// if they are offscreen.
        /// </summary>
        /// <param name="x">The x value of the specified position.</param>
        /// <param name="y">The y value of the specified position.</param>
        /// <param name="z">The z value of the specified position.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        public bool CanSeeScreen(Position pos) {
            return CanSeeScreen(pos.x, pos.y, pos.z);
        }
        */

        /// <summary>
        /// Determines whether this thing can see the specified position.
        /// </summary>
        /// <param name="x">The x value of the specified position.</param>
        /// <param name="y">The y value of the specified position.</param>
        /// <param name="z">The z value of the specified position.</param>
        /// <returns>True if this thing can see the specified
        /// position, false if it can't.</returns>
        public bool CanSee(ushort x, ushort y, byte z) {
            if (!CanSeeZ(z))
                return false;

            if (this.CurrentPosition.z > z) {
                int offset = this.CurrentPosition.z - z;
                return CanSee((ushort)(x - offset),
                    (ushort)(y - offset));
            } else if (this.CurrentPosition.z < z) {
                int offset = z - this.CurrentPosition.z;
                return CanSee(
                    (ushort)(x + offset), (ushort)(y + offset));
            }

            return CanSee(x, y);
        }

        /// <summary>
        /// Compares this thing with the other thing via comparing
        /// the things' stackpos types.
        /// </summary>
        /// <param name="other">The other thing being comapred.</param>
        /// <returns>Returns negative, 0, or positive if this object is
        /// less than, equal to, or greater than, respectively, the other
        /// object.</returns>
        public int CompareTo(Thing other) {
            int thisValue = (int) GetStackPosType();
            int otherValue =  (int) other.GetStackPosType();
            return otherValue - thisValue;
        }

        /// <summary>
        /// Gets the thing's stackpos level.
        /// </summary>
        /// <returns>The thing's stackpos level.</returns>
        public abstract StackPosType GetStackPosType();

        /// <summary>
        /// Adds itself to the protocol specified via the parameters.
        /// Only to be used when sending map tiles.
        /// </summary>
        /// <param name="proto">The protocol to add to.</param>
        /// <param name="player">The player for whom the add is being done.</param>
        public abstract void AddItself(ProtocolSend proto, Player player);

        /// <summary>
        /// Lets this thing know that a creature's health status has been changed.
        /// </summary>
        /// <param name="creature">The creature for whom the health status
        /// has changed.</param>
        public virtual void UpdateHealthStatus(Creature creature) {
        }

        public virtual void UpdateCreatureLight(Creature creature) {
        }

        /// <summary>
        /// Lets this thing know that a magic effect has occured.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="pos">The position of the ffect.</param>
        public virtual void AddEffect(MagicEffect effect, Position pos) {
        }

        /// <summary>
        /// Lets this thing know that a magic shoot effect has occured.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="origin">The origin of the effect.</param>
        /// <param name="destination">The destination of that effect.</param>
        public virtual void AddShootEffect(byte effect, Position origin,
            Position destination) {
        }

        /// <summary>
        /// Lets this thing know that a creature has changed direction.
        /// </summary>
        /// <param name="direction">The new direction of the creature.</param>
        /// <param name="creature">The creature changing direction.</param>
        /// <param name="stackpos">The creature's stackpos.</param>
        public virtual void UpdateDirection(Direction direction, Creature creature,
            byte stackpos) {
        }

        /// <summary>
        /// Lets this thing know that a creature has updated his outfit.
        /// </summary>
        /// <param name="creature">The creature updating his outfit.</param>
        public virtual void UpdateOutfit(Creature creature) {
        }

        /// <summary>
        /// Lets this thing know that a creature has moved.
        /// </summary>
        /// <param name="direction">The direction that the creature has moved.</param>
        /// <param name="creature">The creature moving.</param>
        /// <param name="oldPos">The old position of the creature.</param>
        /// <param name="newPos">The new position of the creature.</param>
        /// <param name="oldStackpos">The creature's old stackpos.</param>
        /// <param name="newStackpos">The creature's new stackpos.</param>
        public virtual void AddCreatureMove(Direction direction, Creature creature,
            Position oldPos, Position newPos, byte oldStackpos, byte newStackpos) {
        }

        /// <summary>
        /// Lets this thing know that a thing has been removed.
        /// </summary>
        /// <param name="position">The position of the thing removed.</param>
        /// <param name="stackpos">The removed thing's stackpos.</param>
        public virtual void RemoveThing(Position position, byte stackpos) {
        }

        /// <summary>
        /// Lets this thing know that a creature has been added to the screen.
        /// </summary>
        /// <param name="creature">The creature being added.</param>
        /// <param name="position">The creature's position.</param>
        /// <param name="stackpos">The creature's stackpos.</param>
        public virtual void AddScreenCreature(Creature creature, Position position,
            byte stackpos) {
        }

        /// <summary>
        /// Moves the things's screen by one, if applicable.
        /// </summary>
        /// <param name="direction">The direction to move the screen.</param>
        /// <param name="oldPos">Creature's old position.</param>
        /// <param name="newPos">Creature's new position.</param>
        /// <param name="gameMap">A reference to the game map.</param>
        public virtual void AddScreenMoveByOne(Direction direction,
            Position oldPos, Position newPos, Map gameMap) {
        }

        /// <summary>
        /// Lets this thing know that a creature has been added to the screen.
        /// </summary>
        /// <param name="creature">The creature being added.</param>
        /// <param name="position">The creature's position.</param>
        /// <param name="stackpos">The creature's stackpos.</param>
        public virtual void AddTileCreature(Creature creature, Position position,
            byte stackpos) {
        }

        /// <summary>
        /// Lets this thing know that an item has been added to the screen.
        /// </summary>
        /// <param name="item">The item that has been added.</param>
        /// <param name="position">The item's position.</param>
        /// <param name="stackpos">The item's stackpos.</param>
        public virtual void AddItem(Item item, Position position,
            byte stackpos) {
        }

        /// <summary>
        /// Thing's current position.
        /// </summary>
        public Position CurrentPosition {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets this thing's name;
        /// </summary>
        public string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets this thing's type.
        /// </summary>
        public uint Type {
            get;
            set;
        }

        /// <summary>
        /// Gets the string when a player looks at this thing.
        /// </summary>
        /// <param name="player">The player looking.</param>
        /// <returns>The look at string.</returns>
        public virtual string GetLookAt(Player player) {
            return "You see "; //Look at string
        }

        /// <summary>
        /// Lets this thing know that a local chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        /// <param name="pos">Position of chat.</param>
        /// <param name="nameFrom">Name of creature doing the chat.</param>
        public virtual void AddLocalChat(ChatLocal chatType, string message, 
            Position pos, Creature creatureFrom) {
        }

        /// <summary>
        /// Lets this thing know that a global chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        /// <param name="nameFrom">Name of creature doing the chat.</param>
        public virtual void AddGlobalChat(ChatGlobal chatType, string message,
            string nameFrom) {
        }

        /// <summary>
        /// Lets this thing know that an anonymous chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        public virtual void AddAnonymousChat(ChatAnonymous chatType, string message) {
        }

        /// <summary>
        /// Lets this thing know that another thing has been added
        /// to the ground.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="position"></param>
        public virtual void AddThingToGround(Thing thing, Position position,
            byte stackpos) {
        }

        /// <summary>
        /// Returns this thing as an item if it is equipable,
        /// returns null if this thing isn't equipable.
        /// </summary>
        /// <param name="inventoryIndex">The inventory index
        /// to see if this thing is equipable for. Note: If this
        /// thing is going to be equipped into a container, use
        /// null as the argument.</param>
        /// <returns>This thing as an item if equipable,
        /// null otherwise.</returns>
        public virtual Item GetEquipableItem() {
            return null;
        }

        /// <summary>
        /// Use the thing.
        /// </summary>
        /// <param name="user">The player using the thing.</param>
        public virtual void UseThing(Player user, GameWorld world) {
        }

        public virtual void UseThingWith(Player user, Position posWith, GameWorld world, 
            byte stackposWith) {
        }

        /// <summary>
        /// Returns whether to let the gameworld proceed with the walk.
        /// True if the gameworld should proceed, false if this thing handles
        /// it fully.
        /// </summary>
        /// <param name="world">A reference to the game world.</param>
        /// <returns></returns>
        public virtual bool HandleWalkAction(Creature creature, GameWorld world, WalkType type) {
            return true;
        }

        public virtual void UpdateItem(Position pos, Item item, byte stackpos) {
        }

        /// <summary>
        /// Performs any special task associated when this thing is moved.      
        /// </summary>
        public virtual void HandleMove() {
        }


        /// <summary>
        /// Gets whether this thing is of the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if this thing is of that type, false otherwise.</returns>
        public bool IsOfType(uint iType) {
            return (Type & iType) != 0;
        }

        public void AddType(uint iType) {
            Type = (Type | iType);
        }

        public virtual void AppendHandleDamage(int dmgAmt, Creature attacker, ImmunityType type,
            GameWorld world, bool magic) {
        }

        public abstract void AppendHandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count, GameWorld world);
    }
}
