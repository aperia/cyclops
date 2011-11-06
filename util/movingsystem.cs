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

/*
 * TODO: Player error messages of why moves aren't valid
 * TODO: Get rid of down casts from Item to Container.
 */
namespace Cyclops {
    /// <summary>
    /// This class handles moving things for the game server.
    /// </summary>
    public class MovingSystem {
        public const int CARRYING_HEADER = 0xFFFF;
        private const int CONTAINER_HEADER = 0x40;
        private const int MAX_STACK_COUNT = 100;
        private const string MSG_NOT_ENOUGH_CAP = "Not enough cap.";
        private const string MSG_THIS_IS_IMPOSSIBLE = "This is impossible.";
        private const string MSG_NOT_ENOUGH_ROOM = "There is no room.";
        private Map map;
        private GameWorld world;

        private Player player;
        private Position posFrom;
        private ushort thingID;
        private byte stackpos;
        private Position posTo;
        private byte count;
        private Item itemToMove;
        private Creature creatureToMove;

        private void SendErrorMessage(string errorMsg) {
            player.AddStatusMessage(errorMsg);
            world.SendProtocolMessages();
        }

        /// <summary>
        /// Construct this object.
        /// </summary>
        /// <param name="gameMap">Reference to the game map.</param>
        public MovingSystem(Map gameMap, GameWorld gameWorld) {
            map = gameMap;
            world = gameWorld;
        }

        /// <summary>
        /// Tests if the specified position indicates
        /// a move to/from a carrying position.
        /// </summary>
        /// <param name="pos">The position to test.</param>
        /// <returns>True if to/from carrying, false otherwise.</returns>
        private bool CarryingPos(Position pos) {
            return pos.x == CARRYING_HEADER;
        }

        /// <summary>
        /// Tests if the specified position indicates
        /// a move to/from a container position.
        /// </summary>
        /// <param name="pos">The position to test.</param>
        /// <returns>True if to/from container, false otherwise.</returns>
        private bool ContainerPos(Position pos) {
            return CarryingPos(pos) && (pos.y & CONTAINER_HEADER) != 0;
        }

        /// <summary>
        /// Tests if the specified position indicates
        /// a move to/from an inventory position.
        /// </summary>
        /// <param name="pos">The position to test.</param>
        /// <returns>True if to/from inventory, false otherwise.</returns>
        private bool InventoryPos(Position pos) {
            return CarryingPos(pos) && !ContainerPos(pos);
        }


        /// <summary>
        /// Move a specified item from the inventory.
        /// </summary>
        /// <param name="thingToMove">The thing to move.</param>
        private void MoveItemFromInventory() {
            byte fromIndex = (byte)posFrom.y;
            if (itemToMove.IsOfType(Constants.TYPE_STACKABLE) && 
                itemToMove.Count != count) {
                Item split = Item.CreateItem(itemToMove.ItemID);
                split.Count = count;
                itemToMove.Count -= count;
                player.RemoveInventoryItem(fromIndex);
                player.AddInventoryItem(fromIndex, itemToMove);
                itemToMove = split;
                //player.UpdateCarryingItem(fromIndex, UpdateCarryingType.INVENTORY, stackable);
                //TODO: UPDATE INVENTORY
            } else {
                player.RemoveInventoryItem(fromIndex);
            }
        }

        /// <summary>
        /// Move a specified item to the inventory.
        /// </summary>
        /// <param name="thingToMove">The thing to move.</param>
        private void MoveItemToInventory() {
            byte toIndex = (byte)posTo.y;
            Item invItem = player.GetInventoryItem(toIndex);
            if (invItem != null) {
                if (itemToMove.IsOfType(Constants.TYPE_STACKABLE) &&
                    itemToMove.ItemID == invItem.ItemID) {
                    invItem.Count += itemToMove.Count;
                    player.RemoveInventoryItem(toIndex);
                    player.AddInventoryItem(toIndex, invItem);
                } else if (invItem.IsOfType(Constants.TYPE_CONTAINER)) {
                    invItem.AddItem(itemToMove);
                } else { //Perform swap....
                    SwapItem(invItem, toIndex);
                }
            } else {
                player.AddInventoryItem(toIndex, itemToMove);
            }
        }

        /// <summary>
        /// Move the specified thing to container.
        /// </summary>
        /// <param name="thingToMove">Thing to move.</param>
        private void MoveItemToContainer() {
            int containerIndex = posTo.y - CONTAINER_HEADER;
            int itemIndex = posTo.z;
            Container container = player.GetContainerByIndex(containerIndex);
            Item containerItem = container.GetItemByIndex(itemIndex);

            bool addItem = true;
            if (containerItem != null && containerItem.IsOfType (Constants.
                TYPE_STACKABLE) && containerItem.ItemID == itemToMove.ItemID) {
                if (containerItem.Count + itemToMove.Count > MAX_STACK_COUNT) {
                    byte countRemainder = (byte)(MAX_STACK_COUNT - containerItem.Count);
                        containerItem.Count += countRemainder;
                        itemToMove.Count -= countRemainder;
                    } else {
                    containerItem.Count += itemToMove.Count;
                        addItem = false;
                    }
            }

            if (containerItem != null && 
                containerItem.IsOfType(Constants.TYPE_CONTAINER)) {
                List<Player> affected = 
                    containerItem.AddItem(itemToMove);
            } else {
                if (addItem) {
                    container.AddItem(itemToMove);
                } else {
                    container.UpdateContainerToViewers();
                }
            }
        }

        /// <summary>
        /// Move the specified thing to the ground.
        /// </summary>
        /// <param name="thingToMove">Thing to move.</param>
        /// <param name="posTo">Position to move to.</param>
        /// <param name="thingsPrepared">A reference to the things
        /// already prepared.</param>
        private void MoveToGround() {
            Thing topThing = map.GetTopThing(posTo);
            Item topItem = null;
            if (topThing.IsOfType(Constants.TYPE_STACKABLE)) {
                topItem = (Item) topThing;
            }

            bool stackable = false;
            bool addItem = true;
            if (topItem != null && itemToMove != null
                && topItem.ItemID == itemToMove.ItemID) { //Both stackable items of same type
                stackable = true;
                if (topItem.Count + itemToMove.Count > MAX_STACK_COUNT) {
                    byte countRemainder = (byte)(MAX_STACK_COUNT - topItem.Count);
                    topItem.Count += countRemainder;
                    itemToMove.Count -= countRemainder;
                } else {
                    topItem.Count += itemToMove.Count;
                    addItem = false;
                }
            }

            ThingSet tSet = map.GetThingsInVicinity(posTo);
            byte stackpos = map.GetStackPosition(itemToMove, posTo);

            if (stackable) {
                foreach (Thing thing in tSet.GetThings()) {
                    thing.UpdateItem(posTo, topItem, 
                        map.GetStackPosition(topItem, posTo));
                }
            }

            if (addItem) {
                map.AddThing(itemToMove, posTo);

                foreach (Thing thing in tSet.GetThings()) {
                    thing.AddThingToGround(itemToMove, posTo,
                        map.GetStackPosition(itemToMove, posTo));
                }
            }
        }

        /// <summary>
        /// Move a specified thing from the ground.
        /// </summary>
        private void MoveItemFromGround() {
            bool stackable = itemToMove.IsOfType(Constants.TYPE_STACKABLE)
                && itemToMove.Count != count;
            Item oldItem = null;
            if (!stackable) {
                map.RemoveThing(itemToMove, posFrom);
            } else {
                oldItem = itemToMove;
                oldItem.Count -= count;
                itemToMove = Item.CreateItem(itemToMove.ItemID);
                itemToMove.Count = count;
            }

            ThingSet tSet = map.GetThingsInVicinity(posFrom);
            foreach (Thing thing in tSet.GetThings()) {
                if (!stackable) {
                    thing.RemoveThing(posFrom, stackpos);
                } else {
                    thing.UpdateItem(posFrom, oldItem, stackpos);
                }
            }
        }

        /// <summary>
        /// Move a thing from container.
        /// </summary>
        private void MoveItemFromContainer() {
            int containerIndex = posFrom.y - CONTAINER_HEADER;
            Container container = player.GetContainerByIndex(containerIndex);
            List<Player> affected = container.GetViewers();
            if (itemToMove.IsOfType(Constants.TYPE_STACKABLE) 
                && itemToMove.Count != count) {
                itemToMove.Count -= count;
                itemToMove = Item.CreateItem(itemToMove.ItemID);
                itemToMove.Count = count;
                container.UpdateContainerToViewers();
            } else {
                container.RemoveItem(itemToMove);
            }
        }

        /// <summary>
        /// Handle moving from a player. Note: The move must be valid
        /// and therefore must be checked before calling this method.
        /// </summary>
        private void HandleItemMove() {
            //Moving from container/inventory
            if (ContainerPos(posFrom)) { //From container
                MoveItemFromContainer();
            } else if (InventoryPos(posFrom)) { //From inventory
                MoveItemFromInventory();
            } else {
                MoveItemFromGround();
            }

            //Moving to container/inventory
            if (ContainerPos(posTo)) {
                    MoveItemToContainer();
            } else if (InventoryPos(posTo)) {
                MoveItemToInventory();
            } else { //Moving to ground
                MoveToGround();
            }

            player.AddStats(); //For cap
            
            //Let thing do any moving related things also
            itemToMove.HandleMove();

            world.SendProtocolMessages();
        }

        /// <summary>
        /// Gets whether a move is valid to the player's inventory.
        /// </summary>
        /// <param name="invIndex">The inventory index.</param>
        /// <param name="item">The item to test.</param>
        /// <returns>True if the move is valid, false otherwise.</returns>
        private bool IsToInventoryValid(byte invIndex, Item item) {
            if (player.GetWeapon() != item) {
                if (player.IsTwoHanded() && player.GetInventoryItem(invIndex) == null &&
                    (invIndex == Constants.INV_RIGHT_HAND || 
                    invIndex == Constants.INV_LEFT_HAND)) {
                    return false;
                }

                if (item.IsTwoHanded() && !player.AreHandsEmpty()) {
                    return false;
                }
            }

            if (invIndex == Constants.INV_RIGHT_HAND ||
                invIndex == Constants.INV_LEFT_HAND) {
                return true;
            }

            string attr = item.GetAttribute(Constants.ATTRIBUTE_SLOT_TYPE);
            if (attr != null) {
                byte slot = Item.GetSlot(attr);
                if (invIndex == slot) {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Swap the items. Note: The posTo must be to an inventory location
        /// and the posFrom must not be an inventory item and the items
        /// to swap must both be equipable.
        /// </summary>
        /// <param name="invItem">The inventory item to swap.</param>
        /// <param name="invIndex">The index for which to swap in the inventory.</param>
        private void SwapItem(Item invItem, byte invIndex) {
            byte origCount = count;
            Item otherItem = itemToMove;
            Position oldPos = posFrom.Clone();
            posFrom = posTo;
            itemToMove = invItem;
            count = invItem.Count;
            MoveItemFromInventory();


            itemToMove = invItem;
            count = invItem.Count;
            posTo = oldPos;
            if (ContainerPos(posTo)) {
                MoveItemToContainer();
            } else {
                MoveToGround();
            }

            posTo = posFrom;
            itemToMove = otherItem;
            count = origCount;
            MoveItemToInventory();

            /*Thing thingToMove = otherItem;
            count = otherItem.Count;
            Position temp = posFrom.Clone();
            if (ContainerPos(posFrom)) {
                MoveFromContainer(ref thingToMove);
            } else { //From ground
                Console.WriteLine("from ground called");
                MoveFromGround(ref thingToMove);
            }
            /*
            thingToMove = invItem;
            count = invItem.Count;
            posFrom.y = invIndex;
            MoveFromInventory(ref thingToMove);
            Position temp2 = posTo.Clone();
            posTo = temp.Clone();
            if (ContainerPos(posTo)) {
                MoveToContainer(thingToMove);
            } else {
                MoveToGround(thingToMove);
            }
            thingToMove = otherItem;
            posTo = temp2.Clone();
            MoveToInventory(thingToMove);
            foreach (Thing thing in thingsPrepared) {
                thing.Finish();
            }*/
        }

        /// <summary>
        /// Gets whether a given move is valid. Note:
        /// this method attemps to make a move valid such as a stackable being
        /// moved to inventory.
        /// </summary>
        /// <returns>True if the move is valid, false otherwise.</returns>
        private bool IsItemMoveValid() {
            /* This Method is a bit of a mess and hence is probably very
             * prone to bugs if not careful. However, I'm being extra careful 
             * to ensure proper validation to avoid item dupe bugs. */
            if (!itemToMove.IsOfType(Constants.TYPE_MOVEABLE) ||
                itemToMove.Count < count) {
                return false;
            }

            double weightToSubtract = 0;
            if (ContainerPos(posFrom)) { //From container
            } else if (InventoryPos(posFrom)) { //From inventory
                weightToSubtract += itemToMove.GetWeight();
            } else { //From ground                
                Tile tile = map.GetTile(posFrom);
                if (tile == null) {
                    return false;
                }

                if (!player.IsNextTo(posFrom)) {
                    player.CurrentWalkSettings.Destination = posFrom;
                    player.CurrentWalkSettings.IntendingToReachDes = false;
                    player.CurrentDelayedAction = new MoveItemDelayed
                        (player, posFrom, thingID, stackpos, posTo, count);
                    return false;
                }
            }


            if (ContainerPos(posTo)) { //To container
                int containerIndex = posTo.y - CONTAINER_HEADER;
                Container container = player.GetContainerByIndex(containerIndex);
                if (container == null) {
                    return false;
                }

                if (player.HasSpecificThing(container) && 
                    itemToMove.GetWeight() > player.GetCurrentCap()) {
                        SendErrorMessage(MSG_NOT_ENOUGH_CAP);
                        return false;
                }

                if (itemToMove == container || container.HasAnyParent(itemToMove)) {
                    SendErrorMessage(MSG_THIS_IS_IMPOSSIBLE);
                    return false;
                }
                Item item = container.GetItemByIndex(posTo.z);
                if (item != null && item.IsOfType(Constants.TYPE_CONTAINER)) {
                    if (!item.HasRoom()) {
                        SendErrorMessage(MSG_NOT_ENOUGH_ROOM);
                        return false;
                    } else {
                        return true;
                    }
                }

                if (!container.HasRoom()) {
                    SendErrorMessage(MSG_NOT_ENOUGH_ROOM);
                    return false;
                }
            } else if (InventoryPos(posTo)) { //To inventory
                byte toIndex = (byte)posTo.y;

                if (itemToMove.GetWeight() - weightToSubtract > player.GetCurrentCap()) {
                    SendErrorMessage(MSG_NOT_ENOUGH_CAP);
                    return false;
                }

                if (player.GetInventoryItem(toIndex) != null) {
                    Item invItem = player.GetInventoryItem(toIndex);
                    if (invItem.ItemID == itemToMove.ItemID) {
                        if (invItem.Count + itemToMove.Count > MAX_STACK_COUNT) {
                            count = (byte)(MAX_STACK_COUNT - invItem.Count);
                        }
                    }
                    if (invItem.IsOfType(Constants.TYPE_CONTAINER)) {
                        if (!invItem.HasRoom()) {
                            SendErrorMessage(MSG_NOT_ENOUGH_ROOM);
                            return false;
                        } else {
                            return true;
                        }
                    }
                    if (InventoryPos(posFrom)) {
                        return false;
                    }
                }

                if (!IsToInventoryValid(toIndex, itemToMove)) {
                    return false;
                }
            } else { //Moving to ground
                if (!player.CanSee(posTo)) {
                    return false;
                }
                return !map.TileContainsType(posTo, Constants.TYPE_BLOCKS_ITEM);
            }
            return true;
        }

        private void HandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count) {
            this.player = player;
            this.posFrom = posFrom;
            this.thingID = thingID;
            this.stackpos = stackpos;
            this.posTo = posTo;
            this.count = count;
        }

        public void HandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count, Creature creatureMoved) {
            HandlePush(player, posFrom, thingID, stackpos, posTo, count);
            creatureToMove = creatureMoved;
            if (!creatureMoved.IsOfType(Constants.TYPE_MOVEABLE) 
                || map.GetTile(posTo) == null) {
                return;
            }
            if (map.GetTile(posTo).ContainsType(Constants.TYPE_BLOCKS_AUTO_WALK) 
                || !creatureToMove.IsNextTo(posTo) || !player.IsNextTo(posFrom)) {
                return;
            }
            world.HandleMove(creatureMoved, posTo, creatureToMove.CurrentDirection);
        }

        /// <summary>
        /// Handle moving from a player. Note: This method is not thread-safe!
        /// </summary>
        /// <param name="player">The player moving the thing.</param>
        /// <param name="posFrom">The position where the thing current is.</param>
        /// <param name="thingID">The thing's id.</param>
        /// <param name="stackpos">The thing's stackpos.</param>
        /// <param name="posTo">The new position to place the item.</param>
        /// <param name="count">How much of the thing to move, if applicable.</param>
        public void HandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count, Item itemMoved) {
            HandlePush(player, posFrom, thingID, stackpos, posTo, count);
            itemToMove = itemMoved;

            //TODO: Finish validating moves...
            if (!IsItemMoveValid()) {
                return;
            }

            HandleItemMove();
        }

        /// <summary>
        /// Gets the thing with the specified parameters or
        /// null if such a thing can't be found. Note: The thing
        /// must be visible for the player.
        /// </summary>
        /// <param name="player">The player for whom to get the item.</param>
        /// <param name="pos">The position of the item.</param>
        /// <param name="stackpos">The stackposition of the thing.</param>
        /// <param name="usePosZ">Use position.z for index instead of stackpos.</param>
        /// <returns>The thing or null if can't be found.</returns>
        public Thing GetThing(Player player, Position pos, byte stackpos, bool usePosZ) {
            //container/inventory
            if (CarryingPos(pos)) {
                if (ContainerPos(pos)) { //From container
                    int containerIndex = pos.y - CONTAINER_HEADER;
                    int itemIndex = usePosZ ? pos.z : stackpos;
                    Container container = player.GetContainerByIndex(containerIndex);
                    if (container == null) {
                        return null;
                    }
                    return container.GetItemByIndex(itemIndex);
                } else { //inventory
                    return player.GetInventoryItem((byte)pos.y);
                }
            } else if (player.CanSee(pos)) { //ground
                if (stackpos == Constants.STACKPOS_TOP_ITEM) {
                    return map.GetTopThing(pos);
                } else {
                    Tile tile = map.GetTile(pos);
                    if (tile == null) {
                        return null;
                    }
                    return tile.GetThing(stackpos);
                }
            } 

            return null;
        }

        /// <summary>
        /// See GetThing()'s overloaded method.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="?"></param>
        /// <param name="stackpos"></param>
        /// <returns></returns>
        public Thing GetThing(Player player, Position pos, byte stackpos) {
            return GetThing(player, pos, stackpos, false);
        }

    }
}
