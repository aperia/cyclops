using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Cyclops {
    public class Container : Item {
        private List<Item> containerItems;
        
        //A list of players who have this container open
        private List<Player> viewers;

        /// <summary>
        /// Construct this object with the following ID and max capacity.
        /// </summary>
        /// <param name="ID">The ID of this container.</param>
        /// <param name="maxCap">The maximimum capacity of this container.</param>
        public Container(ushort ID, byte maxCap)
            : base(ID) {
            containerItems = new List<Item>();
            viewers = new List<Player>();
            MaxCap = maxCap;
        }

        /// <summary>
        /// Gets and sets the maximum capacity.
        /// </summary>
        public byte MaxCap {
            get;
            set;
        }

        /// <summary>
        /// Gets a list of all the items contained in this container.
        /// </summary>
        /// <returns>List of all contained items</returns>
        public List<Item> GetItems() {
            return containerItems;
        }

        /// <summary>
        /// Gets whether this item has enough room to hold another item.
        /// </summary>
        /// <returns>True if this item has room, false otherwise.</returns>
        public override bool HasRoom() {
            return (containerItems.Count < MaxCap);
        }

        /// <summary>
        /// Adds the item to this container and updates to all viewers.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public override List<Player> AddItem(Item item) {
            containerItems.Add(item);
            UpdateContainerToViewers();
            item.Parent = this;
            return viewers;
        }

        /// <summary>
        /// Removes the item specified and appends protocol
        /// data to all viewers.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public override void RemoveItem(Item Item) {
            containerItems.Remove(Item);
            UpdateContainerToViewers();
            Item.Parent = null;
        }

        /// <summary>
        /// Update via appending protocol data to all viewers.
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(Item item) {
            foreach (Player player in viewers) {
                byte index = player.GetContainerIndex(this);
                UpdateCarryingType type = index == 0 ? UpdateCarryingType.CONTAINER_ONE
                    : UpdateCarryingType.CONTAINER_TWO;
                player.UpdateCarryingItem((byte)containerItems.IndexOf(item), type, item);
            }
        }

        /// <summary>
        /// Updates this container for its viewers but
        /// does not reset/send data only appends.
        /// </summary>
        public void UpdateContainerToViewers() {
            foreach (Player player in viewers) {
                player.UpdateContainer(this);
            }
        }

        /// <summary>
        /// Returns the list of current viewers.
        /// </summary>
        /// <returns>List of current viewers.</returns>
        public List<Player> GetViewers() {
            return viewers;
        }

        /// <summary>
        /// Use the thing.
        /// </summary>
        /// <param name="user">The player using the thing.</param>
        public override void UseThing(Player user, GameWorld world) {
            user.OpenContainer(this);
        }

        /// <summary>
        /// Returns the number of ammo this item
        /// represents.
        /// </summary>
        /// <returns>Ammo count this item represtents.</returns>
        public override byte GetAmmoCount(string ammoType) {
            byte ammoCount = 0;

            foreach (Item item in containerItems) {
                ammoCount += item.GetAmmoCount(ammoType);
            }
            return ammoCount;
        }

        /// <summary>
        /// Gets the next item which represents ammo based
        /// on the specified ammunition type.
        /// </summary>
        /// <param name="ammoType">The ammunition type</param>
        /// <returns>The next item with the specified ammunition type.</returns>
        public override Item GetNextAmmo(string ammoType) {
            foreach (Item item in containerItems) {
                if (item.GetNextAmmo(ammoType) != null) {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a viewer, without appending/sending protocol information,
        /// to the container.
        /// </summary>
        /// <param name="player"></param>
        public void AddViewer(Player player) {
            viewers.Add(player);
        }

        /// <summary>
        /// Remove a viewer, without appending/sending protocol information,
        /// from the contanier.
        /// </summary>
        /// <param name="player"></param>
        public void RemoveViewer(Player player) {
            viewers.Remove(player);
        }

        /// <summary>
        /// Gets an item by index. Returns null if there
        /// is no item at the specified index.
        /// </summary>
        /// <param name="index">Index to get item at.</param>
        /// <returns>The item at the index or null if no such item.</returns>
        public Item GetItemByIndex(int index) {
            if (index < 0 || index >= containerItems.Count) {
                return null;
            }

            return containerItems[index];
        }


        /// <summary>
        /// Gets the item's total weight, for containers gets
        /// the weight of container plus all items in inside, etc.
        /// </summary>
        /// <returns></returns>
        public override double GetWeight() {
            double weight = base.GetWeight();
            foreach (Item item in containerItems) {
                weight += item.GetWeight();
            }
            return weight;
        }

        
        ///<summary>
        /// Performs any special task associated when this item is moved.      
        /// </summary>
        public override void HandleMove() {
            List<Player> viewersTmp = new List<Player>();
            foreach (Player viewer in viewers) {
                viewersTmp.Add(viewer);
            }
#if DEBUG
            Console.WriteLine("handle move in container called");
#endif
            foreach (Player viewer in viewersTmp) {
                bool notCarrying = !viewer.HasSpecificThing(this);
                bool notNextTo = !viewer.IsNextTo(this);
                Console.WriteLine("notcarrying: " + notCarrying);
                Console.WriteLine("notnextto: " + notNextTo);
                if (notCarrying && notNextTo) {
                    viewer.AppendCloseContainer(this);
                }
            }
            foreach (Item item in containerItems) {
                item.HandleMove();
            }
        }

        /// <summary>
        /// Gets detailed information about this container as a string.
        /// </summary>
        /// <returns>Detailed information.</returns>
        protected override string GetDetailedInformation(Player player) {
            return " (Vol:" + MaxCap + ")";
        }


        /// <summary>
        /// Returns true whether this item is the specified thing
        /// or this item contains the specified thing, false otherwise.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>True if contains, false otherwise.</returns>
        public override bool ContainsItem(Thing thing) {
            if (base.ContainsItem(thing)) {
                return true;
            }

            foreach (Item cItem in containerItems) {
                if (cItem.ContainsItem(thing)) {
                    return true;
                }
            }
            return false;
        }

        public override void SaveItem(BinaryWriter bWriter) {
            base.SaveItem(bWriter);
            bWriter.Write((byte)containerItems.Count);
            for (int i = 0; i < containerItems.Count; i++) {
                containerItems[i].SaveItem(bWriter);
            }
        }

        public override void LoadItem(BinaryReader bReader) {
            base.LoadItem(bReader);
            byte itemCount = bReader.ReadByte();
            for (int i = 0; i < itemCount; i++) {
                AddItem(Item.Load(bReader));
            }
        }

        /// <summary>
        /// Gets the item count of the specified ID or any
        /// contained items.
        /// Returns 0 if this item is not the specified ID
        /// or this item does not contain any items with the specified
        /// ID.
        /// </summary>
        /// <param name="spriteID">The sprite ID to check.</param>
        /// <returns>The item count this item contributes.</returns>
        public override int GetItemCount(ushort ID) {
            int count =  base.GetItemCount(ID);
            foreach (Item item in containerItems) {
                count += item.GetItemCount(ID);
            }
            return count;
        }

        /// <summary>
        /// If this items ID matches the specified ID, this
        /// method subracts the specified count. Otherwise, does nothing.
        /// This method subtracts from the count parameter as it goes.
        /// </summary>
        /// <param name="ID">The ID to subtract from.</param>
        /// <param name="count">The count to subtract.</param>
        public override void SubtractItemCount(ushort ID, ref int count) {
            if (count == 0) {
                return;
            }
            base.SubtractItemCount(ID, ref count);
            List<Item> itemsToRemove = new List<Item>();
            foreach (Item item in containerItems) {
                item.SubtractItemCount(ID, ref count);
                if (item.Count == 0) {
                    itemsToRemove.Add(item);
                }
            }
            foreach (Item item in itemsToRemove) {
                containerItems.Remove(item);
            }
            UpdateContainerToViewers();
        }


    }
}
