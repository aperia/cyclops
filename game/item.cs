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
    public delegate bool WalkDelegate(Item item, Creature walker, GameWorld world, WalkType type);
    public delegate void UseItemDelegate(Item item, Player user, GameWorld world);
    public delegate void UseItemWithDelegate(Item item, Player user, GameWorld world,
    Position posWith, byte stackpos);

    public enum WalkType {
        WALK_ON,
        WALK_OFF
    };

    /* This class contains information about
     * items in the game 
     */
    public class Item : Thing {
        private static Dictionary<ushort, Item> itemDict;
        private static Dictionary<ushort, WalkDelegate> walkItems = 
        new Dictionary<ushort, WalkDelegate>();

        private static Dictionary<ushort, UseItemDelegate> useItems =
            new Dictionary<ushort, UseItemDelegate>();

        private static Dictionary<ushort, UseItemWithDelegate> useItemsWith =
            new Dictionary<ushort, UseItemWithDelegate>();

        private Dictionary<string, string> attributes;

        public Object blah(Object arg) {
            Dictionary<ushort, UseItemWithDelegate> del =
                (Dictionary<ushort, UseItemWithDelegate>)arg;

            del.Add(109, delegate(Item item, Player user, GameWorld world, Position posWith, byte stackpos) {
                         Thing thing = world.GetMovingSystem().GetThing(user, posWith, stackpos);
                         if (thing is Item) {
                             Item itemWith = (Item)thing;
                             if (itemWith.ItemID == 526) {
                                 world.AddMagicEffect(MagicEffect.LOOSE_ENERGY, posWith);
                                 byte fishingSkill = user.GetSkill(Constants.SKILL_FISHING);
                                 double formula = fishingSkill / 200.0 + .85 * (new Random().Next(0, 100) / 100.0);
                                 if (formula > 0.70) {
                                     user.AddCarryingItem(Item.CreateItem(1922));
                                 }
                                 user.AddFishingTry();
                             }
                         }
            });
            return null;
        }

        private static void Compile(Object arg, string path) {
            FileInfo[] fileList = new DirectoryInfo(path).GetFiles();
            DynamicCompile dCompile = new DynamicCompile();
            foreach (FileInfo info in fileList) {
                dCompile.Compile(path + info.Name, arg);
            }
        }


        private static void CompileActions() {
            string itemDir = Config.GetDataPath() + Config.GetItemDirectory();
            Compile(walkItems, itemDir + Config.GetItemActionWalkDir());
            Compile(useItems, itemDir + Config.GetItemActionUseDir());
            Compile(useItemsWith, itemDir + Config.GetItemActionUseWithDir());
        }

        /// <summary>
        /// Get the fluid name for the item. Note: It must be a fluid
        /// item and the fluid type must not be FLUID_NONE.
        /// </summary>
        /// <returns></returns>
        private string GetFluidName() {
            Fluids[] fluids = {
             Fluids.FLUID_BEER, Fluids.FLUID_BLOOD,
             Fluids.FLUID_LEMONADE, Fluids.FLUID_MANAFLUID,
             Fluids.FLUID_MILK, Fluids.FLUID_SLIME,
             Fluids.FLUID_WATER, Fluids.FLUID_WINE
              };
            string[] fluidNames = { "beer", "blood", "lemonade", "mana",
                                      "milk", "slime", "water", "wine"};
            for (int i = 0; i < fluids.Length; i++) {
                if (fluids[i] == FluidType) 
                    return fluidNames[i];
            }

            throw new Exception("Invalid call to GetFluidName()");
        }

        protected Item(ushort ID) {
            attributes = new Dictionary<string, string>();
            ItemID = ID;
            Article = "";
            Type = 0x00;
            Count = 1;
            Parent = null;
            AddType(Constants.TYPE_ITEM);
        }

        /// <summary>
        /// Get the base parent of this item. The base parent
        /// is defined as the parent who doesn't have a parent so
        /// it goes up the list of containers until it finds
        /// such a parent.
        /// </summary>
        /// <returns></returns>
        public Container GetFinalParent() {
            Container finalParent = Parent;
            while (finalParent.Parent != null) {
                finalParent = finalParent.Parent;
            }
            return finalParent;
        }


        /// <summary>
        /// Tests whether any parent of this item has the
        /// specified container. Note: This will look up all parents
        /// up to the base parent.
        /// </summary>
        /// <param name="contaienr">The thing  to test.</param>
        /// <returns>True if has the specified parent, false otherwise.</returns>
        public bool HasAnyParent(Thing thing) {
            Container localParent = Parent;
            while (localParent != null) {
                if (Parent == thing) {
                    return true;
                }
                localParent = localParent.Parent;
            }
            return false;
        }

        /// <summary>
        /// The parent container.
        /// </summary>
        public Container Parent {
            get;
            set;
        }

        /// <summary>
        /// Sends the protocol data for adding itself to the ground.
        /// </summary>
        /// <param name="proto">A reference to the protocol.</param>
        /// <param name="player">The player for whom to add this to.</param>
        /// <param name="stackPos">The stack position of this thing.</param>
        public override void AddThisToGround(ProtocolSend proto, 
            Player player, Position pos, byte stackPos) {
            proto.AddItem(pos, this, stackPos);
        }

        /// <summary>
        /// Load the master item dictionary, so
        /// if we have to create a new item, we look it up in the
        /// master dictionary.
        /// </summary>
        public static void LoadItems() {
            //string itemPath = Config.GetDataPath() + "items/" + "items.dat"; TODO: REMOVE
			string itemPath = Config.GetDataPath() + Config.GetItemDirectory() + "items.dat";
            Dictionary<ushort, Item> dict = new Dictionary<ushort, Item>();
            BinaryReader bReader = new BinaryReader(File.Open(itemPath, FileMode.Open));
            ushort itemCount = bReader.ReadUInt16();
            Console.WriteLine("ItemCount: " + itemCount);

            for (int i = 0; i < itemCount; i++) {
                ushort id = bReader.ReadUInt16(); //item id
                Item item = new Item(id);
                item.Type = bReader.ReadUInt32(); //item type
                item.Article = bReader.ReadString(); //Article, "a", "an", if any
                item.Name = bReader.ReadString(); //item name
                item.LightLevel = bReader.ReadByte();
                item.Speed = bReader.ReadByte(); 
                byte attributeCount = bReader.ReadByte();
                for (int j = 0; j < attributeCount; j++) {
                    string key = bReader.ReadString(); //Key
                    string value = bReader.ReadString(); //Value
                    item.AddAtribute(key, value);
                }
                if (item.GetAttribute(Constants.ATTRIBUTE_CONTAINER_SIZE) != null 
                    && !item.IsOfType(Constants.TYPE_CONTAINER)) {
                    Console.WriteLine("Item: " + item.Name + " is a container"
                        + " but does not have a container type.");
                    }
                if (!dict.ContainsKey(id)) {
                    dict.Add(id, item);
                } /*else {
#if DEBUG
                    Tracer.Println("Warning! Duplicate key: " + id);
#endif
                }*/
            }
            itemDict = dict;
            CompileActions();
        }

        public static bool DoesItemExist(ushort id) {
            return itemDict.ContainsKey(id);
        }

        public static Item CreateItem(ushort id) {
            if (!itemDict.ContainsKey(id)) {
                return null;
            }
            return itemDict[id].Clone(); //TODO: Finish
        }

        /// <summary>
        /// Gets whether this item is a two-handed item or not.
        /// </summary>
        /// <returns>True if this is a two-handed item, false otherwise.</returns>
        public bool IsTwoHanded() {
            string attr = GetAttribute(Constants.ATTRIBUTE_SLOT_TYPE);
            if (attr == null) {
                return false;
            }

            return attr == Constants.SLOT_TYPE_TWO_HANDED;
        }

        /// <summary>
        /// Inefficient method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Item CreateItem(string name) {
            foreach (KeyValuePair<ushort, Item> kvp in itemDict) {
                if (kvp.Value.Name.ToLower() == name.ToLower()) {
                    return kvp.Value.Clone();
                }
            }
            return null;
        }

        public ushort ItemID {
            get;
            set;
        }

        public string Article {
            get;
            set;
        }

        public byte Count {
            get;
            set;
        }

        public byte LightLevel {
            get;
            set;
        }

        public byte Charges {
            get;
            set;
        }

        public Fluids FluidType {
            get;
            set;
        }

        public byte Speed {
            get;
            set;
        }

        public void AddAtribute(string key, string value) {
            attributes[key] = value;
        }

        public override string ToString() {
            return "ID: " + ItemID + " Name: " + Name;
        }

        /// <summary>
        /// The player carrying this item. Note, only sets for
        /// inventory items.
        /// </summary>
        public Player CarryingInventory {
            get;
            set;
        }

        public Item Clone() {
            Item item = null;
            if (itemDict[ItemID].IsOfType(Constants.TYPE_CONTAINER)) {
                string attr = itemDict[ItemID].
                GetAttribute(Constants.ATTRIBUTE_CONTAINER_SIZE);
                byte containerSize = attr == null ? (byte) 0 : byte.Parse(attr);
                item = new Container(ItemID, containerSize);
            } else {
                item = new Item(ItemID);
            }
            Clone(item);
            return item;
        }

        public void Clone(Item item) {
            item.ItemID = ItemID;
            item.Article = Article;
            item.Name = Name;
            item.Type = Type;
            item.attributes.Clear();
            foreach (KeyValuePair<string, string> kvp in attributes) {
                item.AddAtribute(kvp.Key, kvp.Value);
            }
        }

        public override void UseThingWith(Player user, Position posWith, GameWorld world,
            byte stackposWith) {
            string attr = GetAttribute(Constants.ATTRIBUTE_RUNES_SPELL_NAME);
            if (attr != null) {
                Spell spell = Spell.CreateRuneSpell(attr, user, posWith);
                spell.Rune = this;
                spell.UseWithPos = posWith;
                spell.UseWithStackpos = stackposWith;
                world.GetSpellSystem().CastSpell(spell.Name, user, spell, world);
                return;
            }

            if (useItemsWith.ContainsKey(ItemID)) {
                useItemsWith[ItemID](this, user, world, posWith, stackposWith);
            }
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
        public override Item GetEquipableItem() {
            if (IsOfType(Constants.TYPE_EQUIPABLE)) {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Returns the number of ammo this item
        /// represents.
        /// </summary>
        /// <returns>Ammo count this item represents.</returns>
        public virtual byte GetAmmoCount(string ammoType) {
            string attribute = GetAttribute(Constants.ATTRIBUTE_WEAPON_TYPE);
            string type =  GetAttribute(Constants.ATTRIBUTE_AMMO_TYPE);
            if (attribute == Constants.WEAPON_TYPE_AMMUNITION &&
                type == ammoType) {
                return Count;
            }
            return 0;
        }

        /// <summary>
        /// Gets the first item which is the ammo to be used
        /// by a distance weapon.
        /// </summary>
        /// <param name="ammoType"></param>
        /// <returns></returns>
        public virtual Item GetNextAmmo(string ammoType) {
            string attribute = GetAttribute(Constants.ATTRIBUTE_WEAPON_TYPE);
            string type = GetAttribute(Constants.ATTRIBUTE_AMMO_TYPE);
            if (attribute == Constants.WEAPON_TYPE_AMMUNITION &&
                type == ammoType) {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Gets whether this item has enough room to hold another item.
        /// </summary>
        /// <returns>True if this item has room, false otherwise.</returns>
        public virtual bool HasRoom() {
            return false;
        }


        /// <summary>
        /// Adds the item to this item and returns the list
        /// of affected players.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>List of affected players.</returns>
        public virtual List<Player> AddItem(Item item) {
            throw new Exception("Adding item to non-container type.");
        }


        /// <summary>
        /// Removes the item to this item.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public virtual void RemoveItem(Item Item) {
            throw new Exception("Removing item from non-container type.");
        }

        /// <summary>
        /// Returns true whether this item is the specified thing
        /// or this item contains the specified thing, false otherwise.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>True if contains, false otherwise.</returns>
        public virtual bool ContainsItem(Thing thing) {
            return (this == thing);
        }

        protected string GetMainDescription(Player player) {
            string addSpace = Article == "" ? "" : " ";
            if (Count > 1) {
                return base.GetLookAt(player) + Count + " " + Name + "s";
            }

            return base.GetLookAt(player) + Article + addSpace + Name;
        }

        protected string AddWeight(Player player) {
            double weight = GetWeight();
            if (weight > 0 && (player.HasSpecificThing(this) || player.IsNextTo(this))) {
                string pronoun = Count > 1 ? " They" : " It";
                return pronoun + " weighs " + GetWeight() + ".0 oz.";
            }
            return "";
        }

        protected string AddDesc(Player player) {
            string desc = GetAttribute(Constants.ATTRIBUTE_DESCRIPTION);
            if (desc != null &&
                (player.HasSpecificThing(this) || player.IsNextTo(this))) {
                return " " + desc + ".";
            }
            return "";
        }

        protected virtual string GetDetailedInformation(Player player) {
            string atk = GetAttribute(Constants.ATTRIBUTE_ATTACK);
            string def = GetAttribute(Constants.ATTRIBUTE_DEFENSE);
            string rune = GetAttribute(Constants.ATTRIBUTE_RUNES_SPELL_NAME);
            if (atk != null && def != null) {
                return " (Atk:" + atk + " Def:" + def + ")";
            }

            string arm = GetAttribute(Constants.ATTRIBUTE_ARMOR);
            if (arm != null) {
                return " (Arm: " + arm + ")";
            }

            if (rune != null) {
                Spell spell = Spell.CreateRuneSpell(rune, player, player.CurrentPosition);
                string desc = " for magic level " + spell.RequiredMLevel + " .";
                desc += " It's an \"" + rune + "\"-spell (" + Charges + "x)";
                return desc;
            }

            if (IsOfType(Constants.TYPE_FLUID_CONTAINER)) {
                if (FluidType != Fluids.FLUID_NONE) {
                    return " of " + GetFluidName();
                } else {
                    return ". It is empty";
                }
            }

            return "";
        }

        /// <summary>
        /// See base.GetLookAt().
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override string GetLookAt(Player player) {
            string look = GetMainDescription(player);
            look += GetDetailedInformation(player);
            look += ".";
            look += AddWeight(player);
            look += AddDesc(player);
#if DEBUG
            look = look + " Item ID: " + ItemID;
#endif
            return look;
        }

        /*
         * Prevariant: Item name must be valid.
         */
        public byte GetWeaponType(string sValue) {
            string[] names = {"sword", "club", "axe",
                                 "distance", "shield", "fist"};

            for (byte i = 0; i < names.Length; i++) {
                if (names[i] == sValue)
                    return i;
            }

            throw new Exception("Invalid item name in "
                + "GetWeaponType(). Name: " + sValue);
        }

        /// <summary>
        /// Returns an item's attribute or null if
        /// the item does not have that attribute.
        /// </summary>
        /// <param name="attribute">The attribute to get.</param>
        /// <returns>The value at the specified attribute, if applicable
        /// or null otherwise.</returns>
        public string GetAttribute(string attribute) {
            if (!attributes.ContainsKey(attribute)) {
                return null;
            }
            return attributes[attribute];
        }

        /// <summary>
        /// Adds itself to the specified protocol.
        /// </summary>
        /// <remarks>This add is only called for map tiles and therefore,
        /// it only needs to know how to add itself to maptiles.</remarks>
        /// <param name="proto">The protocol to add itself to.</param>
        /// <param name="player">The player for whom to add.</param>
        public override void AddItself(ProtocolSend proto, Player player) {
            proto.AddGroundItem(this);
        }

        /// <summary>
        /// Gets the item's stackpos type.
        /// </summary>
        /// <returns>Item's stackpos type.</returns>
        public override StackPosType GetStackPosType() {
            if (IsOfType(Constants.TYPE_ON_TOP)) {
                return StackPosType.TOP_ITEM;
            }
            return StackPosType.REGULAR_ITEM;
        }

        
        /// <summary>
        /// Given a slot type, this method returns the slot
        /// constants associated with that type.
        /// </summary>
        /// <param name="slotType">The slot type.</param>
        /// <returns>A constant associated with that slot.</returns>
        public static byte GetSlot(string slotType) {
            switch (slotType) {
                case Constants.SLOT_TYPE_BACKPACK:
                    return Constants.INV_BACKPACK;
                case Constants.SLOT_TYPE_BODY:
                    return Constants.INV_BODY;
                case Constants.SLOT_TYPE_FEET:
                    return Constants.INV_FEET;
                case Constants.SLOT_TYPE_HEAD:
                    return Constants.INV_HEAD;
                case Constants.SLOT_TYPE_LEGS:
                    return Constants.INV_LEGS;
                case Constants.SLOT_TYPE_NECKLACE:
                    return Constants.INV_NECK;
                default:
                    return Constants.INV_LEFT_HAND;
            }
        }

        /// <summary>
        /// Given a weapon type, this method returns
        /// the skill constant associated with that weapon type.
        /// </summary>
        /// <param name="weaponType"></param>
        /// <returns></returns>
        public static byte GetSkillType(string weaponType) {
            switch (weaponType) {
                case Constants.WEAPON_TYPE_AMMUNITION:
                    throw new NotImplementedException(); //TODO: Finish
                case Constants.WEAPON_TYPE_AXE:
                    return Constants.SKILL_AXE;
                case Constants.WEAPON_TYPE_CLUB:
                    return Constants.SKILL_CLUB;
                case Constants.WEAPON_TYPE_DISTANCE:
                    return Constants.SKILL_DISTANCE;
                case Constants.WEAPON_TYPE_SHIELD:
                    return Constants.SKILL_SHIELDING;
                case Constants.WEAPON_TYPE_SWORD:
                    return Constants.SKILL_SWORD;
            }
            throw new Exception("Invalid weaponType in GetSkillType()");
        }

        /// <summary>
        /// Given a shootType, this method returns the distance type
        /// associated with that shoot type.
        /// </summary>
        /// <param name="shootType">The shoot type.</param>
        /// <returns>Associated distance type.</returns>
        public static DistanceType GetDistanceType(string shootType) {
            switch (shootType) {
                case Constants.SHOOT_TYPE_ARROW:
                    return DistanceType.EFFECT_ARROW;
                case Constants.SHOOT_TYPE_BOLT:
                    return DistanceType.EFFECT_BOLT;
                case Constants.SHOOT_TYPE_BURST_ARROW:
                    return DistanceType.EFFECT_BURST_ARROW;
                case Constants.SHOOT_TYPE_ENERGY:
                    return DistanceType.EFFECT_ENERGY;
                case Constants.SHOOT_TYPE_POISON_ARROW:
                    return DistanceType.EFFECT_POISON_ARROW;
                case Constants.SHOOT_TYPE_POWER_BOLT:
                    return DistanceType.EFFECT_POWER_BOLT;
                case Constants.SHOOT_TYPE_SMALL_STONE:
                    return DistanceType.EFFECT_SMALL_STONE;
                case Constants.SHOOT_TYPE_SPEAR:
                    return DistanceType.EFFECT_SPEAR;
                case Constants.SHOOT_TYPE_THROWING_KNIFE:
                    return DistanceType.EFFECT_THROWING_KNIFE;
                case Constants.SHOOT_TYPE_THROWING_STAR:
                    return DistanceType.EFFECT_THROWING_STAR;
                case Constants.SHOOT_TYPE_SNOW_BALL:
                    throw new NotImplementedException(); //TODO: Finish
            }
            throw new Exception("Invalid shootType in GetDistanceType()");
        }

        /// <summary>
        /// Gets the item's total weight, for containers gets
        /// the weight of container plus all items in inside, etc.
        /// </summary>
        /// <returns></returns>
        public virtual double GetWeight() {
            string attr = GetAttribute(Constants.ATTRIBUTE_WEIGHT);
            if (attr == null) {
                return 0;
            }
            return (int.Parse(attr) / 100) * Count;
        }

        public static Item Load(BinaryReader bReader) {
            ushort ID = bReader.ReadUInt16();
            Item item = Item.CreateItem(ID);
            item.LoadItem(bReader);
            return item;
        }

        public virtual void SaveItem(BinaryWriter bWriter) {
            bWriter.Write((ushort)ItemID);
            bWriter.Write((byte)Count);
        }

        public virtual void LoadItem(BinaryReader bReader) {
            Count = bReader.ReadByte();
        }

        /// Returns whether to let the gameworld proceed with the walk.
        /// True if the gameworld should proceed, false if this thing handles
        /// it fully.
        /// </summary>
        /// <param name="player">The player walking.</param>
        /// <param name="world">A reference to the game world.</param>
        /// <returns></returns>
        public override void UseThing(Player user, GameWorld world) {
            if (useItems.ContainsKey(this.ItemID)) {
                useItems[ItemID].Invoke(this, user, world);
            }
        }

        public override bool HandleWalkAction(Creature creature, GameWorld world, WalkType type) {
            if (walkItems.ContainsKey(this.ItemID)) {
                return walkItems[ItemID](this, creature, world, type);
            }
            return true;
        }

        

        /// <summary>
        /// Gets the item count of the specified ID.
        /// Returns 0 if this item is not the specified ID
        /// or this item does not contain any items with the specified
        /// ID.
        /// </summary>
        /// <param name="spriteID">The sprite ID to check.</param>
        /// <returns>The item count this item contributes.</returns>
        public virtual int GetItemCount(ushort ID) {
            return (ItemID == ID) ? System.Convert.ToInt32(Count) : 0;
        }

        /// <summary>
        /// If this items ID matches the specified ID, this
        /// method subracts the specified count. Otherwise, does nothing.
        /// This method subtracts from the count as it goes.
        /// </summary>
        /// <param name="ID">The ID to subtract from.</param>
        /// <param name="count">The count to subtract.</param>
        public virtual void SubtractItemCount(ushort ID, ref int count) {
            if (count == 0) {
                return;
            }

            if (ItemID == ID) {
                if (count >= Count) {
                    count -= Count;
                    Count = 0;
                } else {
                    Count -= (byte)count;
                    count = 0;
                }
            }
        }
        public override void AppendHandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count, GameWorld world) {
            world.GetMovingSystem().HandlePush(player, posFrom, thingID, stackpos, posTo, count, this);
        }
    }
}
