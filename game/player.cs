using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;

// TODO:REMOVE
using System.Xml; 
using System.Xml.Serialization;

namespace Cyclops {
    //TODO: Finish
    public class Player : Creature {
        //Used for global locking between classes
        private static object lockStatic = new object();

        //Used for sending protocol information to the player
        private ProtocolSend protocolS;
        private object lockThis;
        double[,] skillMultipliers; /* Used in determining when to advance next skill */
        double[] manaMultipliers; /* Used in determining when to advance next magic level */
        private int[] skillBase; /* Used in determining when to advanced next skill */
        private uint[] skillTries; //Number of hits done per skill
        private byte[] hpRegeneration; //Used in determining hp regeneration speed
        private byte[] manaRegeneration; //Used in determining mana regeneration speed
        private byte[] skills;
        private Item[] inventory;
        private Queue<uint> knownCreatures;
        private Container[] openContainers;

        /// <summary>
        /// Given a skill, this method returns its name.
        /// </summary>
        /// <param name="skill">The skill to test.</param>
        /// <returns>The skill name.</returns>
        private string GetSkillName(byte skill) {
            switch (skill) {
                case Constants.SKILL_AXE:
                    return "axe fighting";
                case Constants.SKILL_CLUB:
                    return "club fighting";
                case Constants.SKILL_SWORD:
                    return "sword fighting";
                case Constants.SKILL_FIST:
                    return "fist fighting";
                case Constants.SKILL_DISTANCE:
                    return "distance fighting";
                case Constants.SKILL_SHIELDING:
                    return "shielding";
                case Constants.SKILL_FISHING:
                    return "fishing";
            }

            throw new Exception("Invalid skill in GetSkillName()");
        }


        private string GetVocationByName(Vocation vocation) {
            switch (vocation) {
                case Vocation.DRUID:
                    return "druid";
                case Vocation.KNIGHT:
                    return "knight";
                case Vocation.NONE:
                    return "no vocation";
                case Vocation.PALADIN:
                    return "paladin";
                case Vocation.SORCERER:
                    return "sorcerer";
                default:
                    throw new Exception("Invalid call to GetVocationByName()");
            }
        }
		
		// TODO: REMOVE
        /*/// <summary>
        /// Gets the player directory on the file system.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetPlayerPath(string name) {
            string path = Config.GetDataPath();
            path += "players/" + name + ".bin";
            return path;
        }*/

        //Returns a used one if a free one is not avail.
        private byte GetFreeContainerIndex() {
            return (byte)(openContainers[0] == null ? 0 : 1);
        }

        /// <summary>
        /// Adds this player to the affected list and resets the player's
        /// network messages, if not already in list. If in list, does nothing.
        /// </summary>
        private void AddAffected() {
            World.AddAffectedPlayer(this);
        }
        /// <summary>
        /// Initialize the skill multipliers used by the player.
        /// </summary>
        private void InitializeSkillMultipliers() {
            double[] multipliers = { 2.0, 2.0, 2.0, 2.0, 1.5, 1.5, 1.1 };
            for (int skill = 0; skill < multipliers.Length; skill++)
                skillMultipliers[(int)Vocation.NONE, skill] = multipliers[skill];

            multipliers = new double[] { 2.0, 2.0, 2.0, 2.0, 1.5, 1.5, 1.1 };
            for (int skill = 0; skill < multipliers.Length; skill++)
                skillMultipliers[(int)Vocation.SORCERER, skill] = multipliers[skill];

            multipliers = new double[] { 1.8, 1.8, 1.8, 1.8, 1.5, 1.5, 1.1 };
            for (int skill = 0; skill < multipliers.Length; skill++)
                skillMultipliers[(int)Vocation.DRUID, skill] = multipliers[skill];

            multipliers = new double[] { 1.2, 1.2, 1.2, 1.1, 1.1, 1.2, 1.1 };
            for (int skill = 0; skill < multipliers.Length; skill++)
                skillMultipliers[(int)Vocation.PALADIN, skill] = multipliers[skill];

            multipliers = new double[] { 1.1, 1.1, 1.1, 1.4, 1.1, 1.1, 1.1 };
            for (int skill = 0; skill < multipliers.Length; skill++)
                skillMultipliers[(int)Vocation.KNIGHT, skill] = multipliers[skill];
        }

        private void InitializeRegeneration() {
            //----Old Regeneration formula-----
            /* Disclaimer: The following information is not guaranteed
             * to be correct.
             * Format:
             * Vocation : hp: XX hp/YY second(s), mana: AA mana/BB second(s)
             * Example: Sorcerer, hp: 1/12, mana: 1/12 means
             * that the sorcerer regains 1 hitpoint every 12 seconds and 
             * 1 mana every 6 seconds
             * 
             * Sorcerer: hp: 1/12, mana: 1/6
             * Druid: hp: 1/12, mana: 1/6
             * Knight: hp: 1/6, mana: 1/12
             * Paladin: hp: 1/8, 1/8
            */
            //----End of old regeneration formula-----

            hpRegeneration = new byte[(int)Vocation.TOTAL];
            hpRegeneration[(int)Vocation.NONE] = 12;
            hpRegeneration[(int)Vocation.DRUID] = 12;
            hpRegeneration[(int)Vocation.SORCERER] = 12;
            hpRegeneration[(int)Vocation.PALADIN] = 8;
            hpRegeneration[(int)Vocation.KNIGHT] = 6;

            manaRegeneration = new byte[(int)Vocation.TOTAL];
            manaRegeneration[(int)Vocation.NONE] = 12;
            manaRegeneration[(int)Vocation.DRUID] = 6;
            manaRegeneration[(int)Vocation.SORCERER] = 6;
            manaRegeneration[(int)Vocation.PALADIN] = 8;
            manaRegeneration[(int)Vocation.KNIGHT] = 12;
        }

        /// <summary>
        /// Initialize the advance factors used in determining player advancements.
        /// </summary>
        private void InitializeAdvanceFactors() {
            skillBase = new int[] { 50, 50, 50, 30, 100, 50, 20 };
            skillMultipliers = new double[5, 7];
            InitializeSkillMultipliers();
            manaMultipliers = new double[(int)Vocation.TOTAL];
            manaMultipliers[(int)Vocation.NONE] = 4.0;
            manaMultipliers[(int)Vocation.DRUID] = 1.1;
            manaMultipliers[(int)Vocation.SORCERER] = 1.1;
            manaMultipliers[(int)Vocation.PALADIN] = 1.4;
            manaMultipliers[(int)Vocation.KNIGHT] = 3.0;

        }

        /// <summary>
        /// Gets the damage factor used in damage calculation
        /// based on the player's fight mode.
        /// </summary>
        /// <returns></returns>
        private int GetDamageFactor() {
            switch (CurrentFightMode) {
                case FightMode.DEFENSIVE:
                    return 5;
                case FightMode.NORMAL:
                    return 7;
                case FightMode.OFFENSIVE:
                    return 10;
            }

            throw new Exception("CurrentFightMode is invalid in GetDamageFactor()");
        }

        /// <summary>
        /// Adds a skill try to the specified skill.
        /// </summary>
        private void AddSkillTry(byte skillType) {
            uint reqTries = GetReqSkillTries(skillType, skills[skillType]);
            skillTries[skillType] += Config.GetSkillRate();
            if (skillTries[skillType] >= reqTries) {
                skills[skillType]++; //Advance skill
                protocolS.AddSkills(this);
                AddAnonymousChat(ChatAnonymous.WHITE,
                    "You advanced in " + GetSkillName(skillType) + ".");
            }
        }

        private ushort FoodEaten {
            get;
            set;
        }

        /// <summary>
        /// Add any items this player loses to its corpse.
        /// </summary>
        /// <param name="corpse">The corpse for which to add
        /// items.</param>
        private void AddLootItems(Item corpse) {
            for (byte i = 1; i < Constants.INV_MAX; i++) {
                Item invItem = inventory[i];
                if (invItem == null) {
                    continue;
                }
                if (invItem.IsOfType(Constants.TYPE_STACKABLE) ||
                    invItem.IsOfType(Constants.TYPE_CONTAINER)) {
                    corpse.AddItem(invItem);
                    inventory[i] = null;
                    protocolS.RemoveInventoryItem(i);
                    continue;
                }

                int randomChance = rand.Next(0, 10);

                if (randomChance == 0) {
                    corpse.AddItem(invItem);
                    inventory[i] = null;
                    protocolS.RemoveInventoryItem(i);
                }
            }
        }
    

        /// <summary>
        /// Perform the loss effect after dying. Only appends protocol data.
        /// </summary>
        private void DoDeathLoss() {
            double percentLeftAfterDeath = 0;

            //TODO: base percentLeftAfterDeath based on 'promoted' etc.
            percentLeftAfterDeath = 1.0 - (Constants.DEATH_RATE / 100.0);
            Experience = (uint)(Experience * percentLeftAfterDeath);
            while (Experience < GetExperienceNeededForLevel(Level)) {
                DecreaseLevel();
            }

            ManaSpent = (uint)(ManaSpent * percentLeftAfterDeath);
            while (ManaSpent < GetReqMana(MagicLevel)) {
                Console.WriteLine("getreqmagiclvL: " + GetReqMana(MagicLevel));
                Console.WriteLine("manaspent: " + ManaSpent);
                MagicLevel--;
            }
            Console.WriteLine("lvl0: " + GetReqMana(0));

            for (uint i = 0; i < Constants.SKILL_MAX; i++) {
                skillTries[i] = (uint)(skillTries[i] * percentLeftAfterDeath);
                while (skillTries[i] < GetReqSkillTries(i, 
                    (uint)(skills[i] - 1))) {
                    skills[i]--;
                }
            }

            protocolS.AddSkills(this);
            protocolS.AddStats(this);
        }

        /// <summary>
        /// Check for any levels gained.
        /// </summary>
        private void CheckForLevelGain() {
            int expNeeded = GetExperienceNeededForLevel(Level + 1.0);
            while (Experience >= expNeeded) {
                AppendIncreaseLevel();
                expNeeded = GetExperienceNeededForLevel(Level + 1.0);
            }

        }

        /// <summary>
        /// Decrease a player's level and add the message to the player.
        /// </summary>
        public void DecreaseLevel() {
            string advanceMsg =
             "You were downgraded from level " + Level;

            Level = ((byte)(Level - 1));

            advanceMsg += " to level " + Level + ".";

            int hitPointDecrease = 0;
            int manaDecrease = 0;
            int capacityDecrease = 0;

            switch (CurrentVocation) {
                case Vocation.KNIGHT:
                    hitPointDecrease = 15;
                    manaDecrease = 5;
                    capacityDecrease = 25;
                    break;
                case Vocation.PALADIN:
                    hitPointDecrease = 10;
                    manaDecrease = 15;
                    capacityDecrease = 20;
                    break;
                case Vocation.DRUID:
                case Vocation.SORCERER:
                    hitPointDecrease = 5;
                    manaDecrease = 30;
                    capacityDecrease = 10;
                    break;
                default:
                    hitPointDecrease = 5;
                    manaDecrease = 5;
                    capacityDecrease = 10;
                    break;
            }
            MaxHP = (ushort)(MaxHP - hitPointDecrease);
            MaxCapacity -= (ushort)capacityDecrease;
            MaxMana = (ushort)(MaxMana - manaDecrease);
            BaseSpeed -= 2;

            AddAnonymousChat(ChatAnonymous.WHITE, advanceMsg);
        }

        /// <summary>
        /// Increase a given player's level by 1 and add message to protocol.
        /// </summary>
        public void AppendIncreaseLevel() {
            string advanceMsg =
             "You advanced from level " + Level;

            Level = ((byte)(Level + 1));

            advanceMsg += " to level " + Level + ".";

            byte hitPointIncrease = 0;
            byte manaIncrease = 0;
            byte capacityIncrease = 0;
            //TODO: Move code to single location
            switch (CurrentVocation) {
                case Vocation.KNIGHT:
                    hitPointIncrease = 15;
                    manaIncrease = 5;
                    capacityIncrease = 25;
                    break;
                case Vocation.PALADIN:
                    hitPointIncrease = 10;
                    manaIncrease = 15;
                    capacityIncrease = 20;
                    break;
                case Vocation.DRUID:
                case Vocation.SORCERER:
                    hitPointIncrease = 5;
                    manaIncrease = 30;
                    capacityIncrease = 10;
                    break;
                default:
                    hitPointIncrease = 5;
                    manaIncrease = 5;
                    capacityIncrease = 10;
                    break;
            }
            MaxHP += hitPointIncrease;
            CurrentHP += hitPointIncrease;
            MaxCapacity += capacityIncrease;
            CurrentCapacity += capacityIncrease;
            MaxMana += manaIncrease;
            CurrentMana += manaIncrease;
            BaseSpeed += 2;
            AddAnonymousChat(ChatAnonymous.WHITE, advanceMsg);
            protocolS.AddSkills(this);
            protocolS.AddStats(this);
        }

        /// <summary>
        /// Get the required mana for the specified magic level advance.
        /// </summary>
        /// <returns>The required mana.</returns>
        private uint GetReqMana(byte mLvl) {
            if (mLvl == 0) {
                return 0;
            }
            uint reqMana = (uint)(400 * Math.Pow(manaMultipliers[
                (int)CurrentVocation], mLvl - 1));
            if (reqMana % 20 < 10) {
                reqMana = reqMana - (reqMana % 20);
            } else {
                reqMana = reqMana - (reqMana % 20) + 20;
            }
            return reqMana;
        }

        /// <summary>
        /// Get the required skill tries for the specified level.
        /// </summary>
        /// <param name="skill">The skill to get the required try for.</param>
        /// <param name="skillLvl">The skill level to try for.</param>
        /// <returns>The required skill tries.</returns>
        private uint GetReqSkillTries(uint skill, uint skillLvl) {
            uint tries = (uint)(skillBase[skill] * Math.Pow((float)
                skillMultipliers[(int)CurrentVocation, skill],
                (float)(skillLvl - 10)));
            return tries;
        }
            
        /// <summary>
        /// Gets the experience needed for the specified level.
        /// </summary>
        /// <param name="level">Level to get experience for.</param>
        /// <returns>Experience needed for specified level.</returns>
        public int GetExperienceNeededForLevel(double level) {
            return (int)(((50.0 * level * level * level) / 3.0)
             - (100.0 * level * level) + ((850.0 * level) / 3.0)
             - 200.0);
        }

        private uint ManaSpent {
            get;
            set;
        }

        private uint GetSkillTries(int index) {
            return skillTries[index];
        }

        private void SetSkill(int index, byte value) {
            skills[index] = value;
        }

        private void SetSkillTries(int index, uint value) {
        }

        /// <summary>
        /// Returns the index constant of the current
        /// skill being used.
        /// </summary>
        /// <returns></returns>
        private byte GetCurrentSkillType() {
            Item weapon = GetWeapon();
            if (weapon == null) {
                return Constants.SKILL_FIST;
            } else {
                string attribute = weapon.GetAttribute
                    (Constants.ATTRIBUTE_WEAPON_TYPE);
                return Item.GetSkillType(attribute);
            }
        }

        /// <summary>
        /// Returns the currently equipped shield or null if none is equipped.
        /// </summary>
        /// <returns>The shield or null if not applicable.</returns>
        private Item GetShield() {
            Item[] items = {inventory[Constants.INV_LEFT_HAND],
                inventory[Constants.INV_RIGHT_HAND]};

            foreach (Item item in items) {
                if (item != null) {
                    string attribute =
                        item.GetAttribute(Constants.ATTRIBUTE_WEAPON_TYPE);

                    if (attribute != null &&
                        attribute == Constants.WEAPON_TYPE_SHIELD) {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an aggregate of all the armor the player is
        /// currently wearing.
        /// </summary>
        /// <returns>Armor value.</returns>
        private int GetArmorValue() {
            Item[] items = 
            {inventory[Constants.INV_HEAD], inventory[Constants.INV_LEGS],
             inventory[Constants.INV_BODY], inventory[Constants.INV_FEET]};
            int armor = 0;
            foreach (Item item in items) {
                if (item == null) {
                    continue;
                }
                string value = item.GetAttribute(Constants.ATTRIBUTE_ARMOR);
                if (value != null) {
                    armor += int.Parse(value);
                }
            }
            return armor;
        }

        private ushort CurrentCapacity {
            get;
            set;
        }

        /// <summary>
        /// Constructs this object.
        /// </summary>
        /// <param name="proto">The protocol used to 
        /// communicate to the player with.</param>
        public Player(ProtocolSend proto) {
            protocolS = proto;
            lockThis = new Object();
            SpellLightLevel = 0x01;
            Level = 1;
            CurrentCapacity = 410;
            CurrentMana = 410;
            MagicLevel = 1;
            Experience = 0;
            InitializeAdvanceFactors();
            InitializeRegeneration();
            skills = new byte[Constants.SKILL_MAX];
            skillTries = new uint[Constants.SKILL_MAX];
            for (int i = 0; i < Constants.SKILL_MAX; i++) {
                skills[i] = 10;
                skillTries[i] = 0;
            }
            inventory = new Item[Constants.INV_MAX];
            for (int i = 0; i < Constants.INV_MAX; i++) {
                inventory[i] = null;
            }
            CurrentFightMode = FightMode.NORMAL;
            knownCreatures = new Queue<uint>();
            openContainers = new Container[Constants.MAX_CONTAINERS];
            AddType(Constants.TYPE_MOVEABLE);
            ManaShield = false;
            BaseSpeed = 220;
        }

        public bool ManaShield {
            get;
            set;
        }

        public ManaShieldCheck ManaShieldCheck {
            get;
            set;
        }

        public void AddManaShield(long timeInCS) {
            ManaShield = true;
            ManaShieldCheck = new ManaShieldCheck();
            ManaShieldCheck.CurrentPlayer = this;
            ManaShieldCheck.World = World;
            World.AddEventInCS(timeInCS, ManaShieldCheck.PerformCheck);
        }

        public byte GetSkill(int index) {
            return skills[index];
        }

        /// <summary>
        /// Returns whether the player is using distance attacks.
        /// </summary>
        /// <returns>True if the player is using distance, false otherwise.</returns>
        public override bool UsingDistance() {
            Item weapon = GetWeapon();
            if (weapon == null) {
                return false;
            }
            string atr = weapon.GetAttribute(Constants.ATTRIBUTE_WEAPON_TYPE);
            if (atr != Constants.WEAPON_TYPE_DISTANCE) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets how much ammo the player currently has.
        /// </summary>
        /// <returns></returns>
        public override byte GetAmmo() {
            if (!UsingDistance()) {
                return 0;
            }
            string ammoType =
                GetWeapon().GetAttribute(Constants.ATTRIBUTE_AMMO_TYPE);
            byte ammo = 0;
            for (int i = 0; i < Constants.INV_MAX; i++) {
                if (inventory[i] != null) {
                    ammo += inventory[i].GetAmmoCount(ammoType);
                }
            }
            return ammo;
        }

        /// <summary>
        /// Returns the currently equipped weapon or null if none is equipped.
        /// </summary>
        /// <returns></returns>
        public Item GetWeapon() {
            Item[] items = {inventory[Constants.INV_LEFT_HAND],
                inventory[Constants.INV_RIGHT_HAND]};

            foreach (Item item in items) {
                if (item != null) {
                    string attribute =
                        item.GetAttribute(Constants.ATTRIBUTE_WEAPON_TYPE);
                    if (attribute != null &&
                        attribute != Constants.WEAPON_TYPE_SHIELD
                        && attribute != Constants.WEAPON_TYPE_AMMUNITION) {
                        return item;
                    }
                }
            }
            return null;
        }


        //Testing functions, don't u fking remove newb...
        public void remove() {
           
            pos = CurrentPosition.Clone();
            pos.x++;
            pos.x -= 5;
            pos.y -= 4;
        }
        public void add() {
            pos = CurrentPosition.Clone();
            pos.x -= 5;
            pos.y -= 4;
        }

        Position pos = null;
        
        private int testa = 0;
        private int testb = 12;
        int countera = 0;
        int counterb = 0;

        public void buggabugga() {
            AddAffected();
            int id = (testb) + (testa++ * 0xFF);
            ((ProtocolSend65)protocolS).buggabugga(id, pos);
            //CurrentVocation = Vocation.SORCERER;
            //HealthStatus = HealthStatus.
            UpdateHealthStatus(this);
            World.SendProtocolMessages();
           // World.AddEventInCS(1 , buggabugga);
        }
        //End testing functions

        /// <summary>
        /// Gets whether the player is wielding a 2-handed sword.
        /// </summary>
        /// <returns>True if player is wielding 2-handed sword, false otherwise.</returns>
        public bool IsTwoHanded() {
            Item item = GetWeapon();
            if (item == null) {
                return false;
            }
            return item.IsTwoHanded();
        }

        /// <summary>
        /// Returns whether the player's hands are free.
        /// </summary>
        /// <returns>True if the player isn't holding anything, false otherwise.</returns>
        public bool AreHandsEmpty() {
            return (inventory[Constants.INV_RIGHT_HAND] == null 
                && inventory[Constants.INV_LEFT_HAND] == null);
        }

        /// <summary>
        /// Gets the distance type the player is using,
        /// or EFFECT_NONE if the player doesn't have ammo or
        /// isn't using distance.
        /// </summary>
        /// <returns></returns>
        public override DistanceType GetDistanceType() {
            if (GetAmmo() == 0) {
                return DistanceType.EFFECT_NONE;
            }
            string ammoType = GetWeapon().GetAttribute(Constants.ATTRIBUTE_AMMO_TYPE);
            for (int i = 0; i < Constants.INV_MAX; i++) {
                if (inventory[i] != null) {
                    Item item = inventory[i].GetNextAmmo(ammoType);
                    if (item != null) {
                        string shootType = item.GetAttribute(Constants.ATTRIBUTE_SHOOT_TYPE);
                        item.Count--;
                        if (item.Count == 0) {
                            World.AppendRemoveItem(item);
                        } else {
                            World.AppendUpdateItem(item);
                        }
                        AddStats();
                        return Item.GetDistanceType(shootType);
                    }
                }
            }
            throw new Exception("Invalid state in GetDistanceType()");
        }

        /// <summary>
        /// Add mana spent for the player.
        /// </summary>
        /// <param name="amt">The ammount of mana spent.</param>
        public void AppendAddManaSpent(uint amt) {
            if (amt == 0) {
                return;
            }
#if DEBUG
			Log.WriteDebug("Player " + Name + " has spent " + amt + " mana.");
#endif
            ManaSpent += amt;
            uint reqMana = GetReqMana((byte)(MagicLevel + 1));
            if (ManaSpent >= reqMana) {
                ManaSpent -= reqMana;
                MagicLevel++;
                AddAnonymousChat(ChatAnonymous.WHITE,
                    "You advanced to magic level " + MagicLevel + ".");
                protocolS.AddSkills(this);
            }
        }

        public string Password {
            get;
            set;
        }

        public ushort MaxCapacity {
            get;
            set;
        }

        public byte Level {
            get;
            set;
        }

        public ushort MaxMana {
            get;
            set;
        }

        public ushort CurrentMana {
            get;
            set;
        }

        public byte MagicLevel {
            get;
            set;
        }

        public uint Experience {
            get;
            set;
        }

        public Vocation CurrentVocation {
            get;
            set;
        }

        /// <summary>
        /// Eats food and if applicable, appends protocol data.
        /// Returns whether the given player is full.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool AppendEatFood(ushort amount) {
            if (FoodEaten == 0) {
                World.AddEventInCS(100, FoodCheck);
            }
            if (FoodEaten + amount >= 1200) {
                AddStatusMessage("You are full.");
                return true;
            }

            FoodEaten = (ushort)(Math.Min(1200, FoodEaten + amount));
            return false;
        }

        public void FoodCheck() {
            if (!LogedIn) {
                FoodEaten = 0;
                return;
            }

            if (FoodEaten == 0) {
                throw new Exception("Invalid call to FoodCheck()");
            }

            if (FoodEaten % manaRegeneration[(int)CurrentVocation] == 0) {
                if (CurrentMana < MaxMana) {
                    CurrentMana++;
                    AddStats();
                }
            }

            if (FoodEaten % hpRegeneration[(int)CurrentVocation] == 0) {
                World.AppendAddDamage(null, this, -1, ImmunityType.IMMUNE_PHYSICAL, true);
            }

            FoodEaten--;
            if (FoodEaten == 0) {
                return;
            }

            World.AddEventInCS(100, FoodCheck);
        }

        public int GetCurrentCap() {
            double weight = 0;
            for (byte i = 1; i < Constants.INV_MAX; i++) {
                Item item = GetInventoryItem(i);
                if (item != null) {
                    weight += item.GetWeight();
                }
            }
            return (int)(MaxCapacity - (int)Math.Ceiling(weight));
        }

        public void ResetStats() {
            CurrentHP = MaxHP;
            CurrentMana = MaxMana;
            CurrentPosition = new Position(32097, 32217, 7); //todo: fix
        }

        public Item GetInventoryItem(byte index) {
            return inventory[index];
        }

        public void SetInventoryItem(Item item, byte index) {
            item.CarryingInventory = this;
            inventory[index] = item;
        }

        /// <summary>
        /// Returns true if the specified creature is known, otherwise
        /// false and adds it to the known creatures queue.
        /// </summary>
        /// <param name="creature">Creature to test</param>
        /// <returns>True if known, false otherwise.</returns>
        public bool KnowsCreature(Creature creature) {
            if (knownCreatures.Contains(creature.GetID()))
                return true;

            if (knownCreatures.Count >= 128)
                knownCreatures.Dequeue();

            knownCreatures.Enqueue(creature.GetID());
            return false;
        }

        public override void NotifyOfAttack() {
            //TODO: Add to its own method
            if (UsingDistance()) {
                //Item item = inventory[Constants.INV_ARROWS];
                //item.Count--;
                //if (item.Count == 0) {
                //    inventory[Constants.INV_ARROWS] = null;
                // }
                // protocolS.AddFullInventory(this);
            }

            byte skillType = GetCurrentSkillType();
            AddSkillTry(skillType);
        }

        public void ResetNetMessages() {
            protocolS.Reset();
        }

        public void WriteToSocket() {
            protocolS.WriteToSocket();
        }

        public override void AddItem(Item item, Position position, byte stackpos) {
            if (CanSee(position)) {
                AddAffected();
                protocolS.AddItem(position, item, stackpos);
            }
        }

        /// <summary>
        /// Lets this player know that a creature has been added to the screen.
        /// Used when sending a new creature to the screen of a player's client.
        /// </summary>
        /// <param name="creature">The creature being added.</param>
        /// <param name="position">The creature's position.</param>
        /// <param name="stackpos">The creature's stackpos.</param>
        public override void AddScreenCreature(Creature creature, Position position,
            byte stackpos) {
                if (CanSee(position)) {
                    AddAffected();
                    protocolS.AddScreenCreature(creature, KnowsCreature(creature), position, stackpos);
                }
        }

        public override void AddDamage(int dmgAmt, Creature attacker, bool magic) {
            if (ManaShield && dmgAmt > 0 && CurrentMana != 0) {
                string loseString = "You lose " + dmgAmt + " manapoints" +
                    (dmgAmt > 1 ? "s" : "") + ".";
                string leftString = " You have " + CurrentMana + " manapoints" +
                    (CurrentHP > 1 ? "s" : "") + " left.";
                AddStatusMessage(loseString + leftString);
                if (dmgAmt > CurrentMana) {
                    dmgAmt = dmgAmt - CurrentMana;
                    CurrentMana = 0;
                } else {
                    CurrentMana = (ushort)(CurrentMana - dmgAmt);
                    dmgAmt = 0;
                }
            }
            base.AddDamage(dmgAmt, attacker, magic);

            if (!magic && GetShield() != null) {
                AddSkillTry(Constants.SKILL_SHIELDING);
            }
            if (dmgAmt != 0) {
                AddStats();
            }
        }


        /// <summary>
        /// Lets this player know that a creature has been to a map,
        /// used when sending a maptile.
        /// </summary>
        /// <param name="creature">The creature being added.</param>
        /// <param name="position">The creature's position.</param>
        /// <param name="stackpos">The creature's stackpos.</param>
        public override void AddTileCreature(Creature creature, Position position,
            byte stackpos) {
            AddAffected();
            protocolS.AddTileCreature(creature, KnowsCreature(creature));
        }

        public override void AddEffect(MagicEffect effect, Position pos) {
            if (CanSee(pos)) {
                AddAffected();
                protocolS.AddEffect(effect, pos);
            }
        }

        public override void AddShootEffect(byte effect, Position origin,
            Position destination) {
                if (CanSee(origin) || CanSee(destination)) {
                    AddAffected();
                    protocolS.AddShootEffect(effect, origin, destination);
                }
        }

        public override void AddCreatureMove(Direction direction, Creature creature,
            Position oldPos, Position newPos, byte oldStackpos, byte newStackpos) {
            if (CanSee(oldPos) && CanSee(newPos)) {
                AddAffected();
                protocolS.AddCreatureMove(direction, creature, oldPos, newPos, oldStackpos, newStackpos);
            } else if (CanSee(newPos)) {
                AddAffected();
                protocolS.AddScreenCreature(creature, KnowsCreature(creature),
                    newPos, newStackpos);
            } else if (CanSee(oldPos)) {
                AddAffected();
                protocolS.RemoveThing(oldPos, oldStackpos);
                protocolS.UpdateCreatureHealth(creature);
            }
        }

        public override void RemoveThing(Position position, byte stackpos) {
            if (CanSee(position)) {
                AddAffected();
                protocolS.RemoveThing(position, stackpos);
            }
        }

        public override void UpdateDirection(Direction direction, 
            Creature creature, byte stackpos) {
                if (CanSee(creature.CurrentPosition)) {
                    AddAffected();
                    protocolS.UpdateCreatureDirection(creature, direction,
                        stackpos, creature.CurrentPosition);
                }
        }

        public override void UpdateHealthStatus(Creature creature) {
            AddAffected();
            protocolS.UpdateCreatureHealth(creature);
        }

        public override void UpdateOutfit(Creature creature) {
            AddAffected();
            protocolS.UpdateCreatureOutfit(creature);
        }

        /// <summary>
        /// Gets the player's atk value.
        /// </summary>
        /// <returns>Player's atk value.</returns>
        public override int GetAtkValue() {
            int atk = 0;
            int dmgFactor = GetDamageFactor();
            int skill = skills[GetCurrentSkillType()];
            Item weapon = GetWeapon();
            if (weapon != null) {
                string atr = weapon.GetAttribute(Constants.ATTRIBUTE_ATTACK);
                if (atr != null) {
                    atk = int.Parse(atr);
                }
            }
#if DEBUG
			Log.WriteDebug("CurrentSkillType: " + GetCurrentSkillType());
            Log.WriteDebug("Current skill level: " + skill);
            Log.WriteDebug("CalculateAttack: "
                + CalculateAttack(atk, skill, dmgFactor));
#endif
            return CalculateAttack(atk, skill, dmgFactor);
        }

        /// <summary>
        /// Gets the player's shield value.
        /// </summary>
        /// <returns>Shield value.</returns>
        public override int GetShieldValue() {
            int defense = skills[Constants.SKILL_SHIELDING];
            Item shield = GetShield();
            if (shield != null) {
                defense +=
                    int.Parse(shield.GetAttribute(Constants.ATTRIBUTE_DEFENSE));
            }
#if DEBUG
            Log.WriteDebug("CalculateShielding: " + CalculateShielding(defense, GetArmorValue()));
#endif
            return CalculateShielding(defense, GetArmorValue());
        }

        /// <summary>
        /// Notify that the player is dead and handle logging
        /// the player out of the game world.
        /// </summary>
        /// <param name="corpse">Items to add to corpse.</param>
        public override void AppendNotifyOfDeath(Item corpse, Map gameMap) {
            AddAnonymousChat(ChatAnonymous.WHITE, "You are dead.");
            DoDeathLoss();
            AddLootItems(corpse);
            World.AppendRemovePlayer(this, true);
            ResetStats();
        }

        /// <summary>
        /// Moves the player's screen by one.
        /// </summary>
        /// <param name="direction">The direction to move the screen.</param>
        /// <param name="oldPos">player's old position.</param>
        /// <param name="newPos">player's new position.</param>
        /// <param name="gameMap">A reference to the game map.</param>
        public override void AddScreenMoveByOne(Direction direction, Position oldPos, Position newPos,
            Map gameMap) {
            AddAffected();
            protocolS.AddScreenMoveByOne(direction, oldPos, newPos, gameMap, this);
        }

        public void AddLoginBytes(Map map) {
            AddAffected();
            protocolS.AddLoginBytes(this, map);
        }

        public void AddRequestOutfit() {
            AddAffected();
            protocolS.AddRequestOutfit(this);
        }

        /// <summary>
        /// Forcibly close the player's connection immediately.
        /// </summary>
        public void Logout() {
            protocolS.Close();
        }

        /// <summary>
        /// Mark the connection as closed. This means that
        /// after the next WriteToSocket() called for player,
        /// the player's socket will be closed.
        /// </summary>
        public void MarkConnectionClosed() {
            protocolS.MarkSocketAsClosed();
        }

        public Item shield() {
            return GetShield();
        }

        /// <summary>
        /// See base.GetLookAt().
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override string GetLookAt(Player player) {
            string youSee = base.GetLookAt(player);
            if (this == player) {
                string yourself = youSee + "yourself. You ";
                if (CurrentVocation == Vocation.NONE) {
                    yourself += "have no vocation";
                } else {
                    yourself += "are a " + GetVocationByName(CurrentVocation);
                }
                yourself += ".";
                return yourself;
            }


            string other = Name + "(Level " + Level + "). ";
            other += "He ";
            if (CurrentVocation == Vocation.NONE) {
                other += "has no vocation";
            } else {
                other += "is a " + GetVocationByName(CurrentVocation);
            }
            other += ".";
            return youSee + other;
        }
		
		public void SavePlayer()
		{
            lock (lockStatic)
			{
				string connectTo = "URI=file:data/players.db";
				
				using (IDbConnection connection = new SqliteConnection(connectTo))
				{
					connection.Open();
					
					if (connection == null || connection.State != ConnectionState.Open)
					{
#if DEBUG
						Log.WriteDebug("Could not open player database: state=" + connection.State);
#endif
					
						return;
					}
					
#if DEBUG
					Log.WriteDebug("Player database loaded.");
#endif
					
					using (IDbCommand command = connection.CreateCommand())
					{
						string sql = "UPDATE players SET";
						//sql += ", name=" + Name;
						//sql += ", password=" + Password;
						sql += " level=" + Level;
						sql += ", experience=" + Experience;
						sql += ", magic_level=" + MagicLevel;
						sql += ", mana_spent=" + ManaSpent;
						sql += ", current_hp=" + CurrentHP;
						sql += ", max_hp=" + MaxHP;
						sql += ", current_mana=" + CurrentMana;
						sql += ", max_mana=" + MaxMana;
						sql += ", current_cap=" + CurrentCapacity;
						sql += ", max_cap=" + MaxCapacity;
						sql += ", access=" + "0"; // fix
						sql += ", gender=" + "0"; // fix
						sql += ", position_x=" + CurrentPosition.x;
						sql += ", position_y=" + CurrentPosition.y;
						sql += ", position_z=" + CurrentPosition.z;
						sql += ", char_type=" + CharType;
						sql += ", outfit_upper=" + OutfitUpper;
						sql += ", outfit_middle=" + OutfitMiddle;
						sql += ", outfit_lower=" + OutfitLower;
						sql += ", skill_fist=" + GetSkill(Constants.SKILL_FIST);
						sql += ", skill_club=" + GetSkill(Constants.SKILL_CLUB);
						sql += ", skill_sword=" + GetSkill(Constants.SKILL_SWORD);
						sql += ", skill_axe=" + GetSkill(Constants.SKILL_AXE);
						sql += ", skill_distance=" + GetSkill(Constants.SKILL_DISTANCE);
						sql += ", skill_shielding=" + GetSkill(Constants.SKILL_SHIELDING);
						sql += ", skill_fishing=" + GetSkill(Constants.SKILL_FISHING);	
						sql += ", skill_tries_fist=" + GetSkillTries(Constants.SKILL_FIST);
						sql += ", skill_tries_club=" + GetSkillTries(Constants.SKILL_CLUB);
						sql += ", skill_tries_sword=" + GetSkillTries(Constants.SKILL_SWORD);
						sql += ", skill_tries_axe=" + GetSkillTries(Constants.SKILL_AXE);
						sql += ", skill_tries_distance=" + GetSkillTries(Constants.SKILL_DISTANCE);
						sql += ", skill_tries_shielding=" + GetSkillTries(Constants.SKILL_SHIELDING);
						sql += ", skill_tries_fishing=" + GetSkillTries(Constants.SKILL_FISHING);
						sql += ", base_speed=" + BaseSpeed;
						sql += ", vocation=" + (int)CurrentVocation;
						
						// Inventory
						Stream invStream = new MemoryStream();
						BinaryWriter bw = new BinaryWriter(invStream);
						
						if (inventory[Constants.INV_NECK] != null) {
							bw.Write((ushort)inventory[Constants.INV_NECK].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_HEAD] != null) {
							bw.Write((ushort)inventory[Constants.INV_HEAD].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_LEFT_HAND] != null) {
							bw.Write((ushort)inventory[Constants.INV_LEFT_HAND].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_RIGHT_HAND] != null) {
							bw.Write((ushort)inventory[Constants.INV_RIGHT_HAND].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_LEGS] != null) {
							bw.Write((ushort)inventory[Constants.INV_LEGS].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_BODY] != null) {
							bw.Write((ushort)inventory[Constants.INV_BODY].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_FEET] != null) {
							bw.Write((ushort)inventory[Constants.INV_FEET].ItemID);
						} else
							bw.Write((ushort)0);
						if (inventory[Constants.INV_BACKPACK] != null)
						{

							
//							bw.Write((ushort)inventory[Constants.INV_BACKPACK].ItemID);
//							Tracer.Println("INV_BACKPACK: " + (ushort)inventory[Constants.INV_BACKPACK].ItemID);
//							Tracer.Println("INV_BACKPACK_TYPE: " + inventory[Constants.INV_BACKPACK].Type.ToString());
//							// Save backpack items
//							if (inventory[Constants.INV_BACKPACK].Type == /*Constants.TYPE_CONTAINER // why doesn't this work?*/ 9220)
//							{
//								Container bpContainer = (Container)inventory[Constants.INV_BACKPACK];
//								bw.Write((byte)bpContainer.GetItems().Count);
//								Tracer.Println("INV_BACKPACK_COUNT: " + (byte)bpContainer.GetItems().Count);
//								List<Item> bpItems = GetItemsInContainer((Container)inventory[Constants.INV_BACKPACK]);
//								
//								foreach (Item i in bpItems)
//								{
//									bw.Write((ushort)i.ItemID);
//									
//									if (i.Type == /*Constants.TYPE_CONTAINER // why doesn't this work?*/ 9220)
//									{
//										Container c = (Container)i;
//										bw.Write((byte)c.GetItems().Count);
//									}
//									
//									Tracer.Println("bp: " + i.Name + " (" + i.ItemID + ")");
//								}
//							}
						} 
						else
							bw.Write((ushort)0);

						// backpack items here!
						
						invStream.Position = 0;
						BinaryReader br = new BinaryReader(invStream);
						
						byte[] invBuffer = br.ReadBytes((int)invStream.Length);
						
						// TODO: REMOVE BASE64, STORE BLOB
						sql += ", inventory='" + Convert.ToBase64String(invBuffer) + "'";
						
						bw.Close();
						invStream.Dispose();
						
						// Close sql command
						sql += " WHERE name = '" + Name + "'";
						
						// EXECUTE!
						command.CommandText = sql;
						command.ExecuteNonQuery();
					}
				}
            }
        }
		
		// TODO: REMOVE
        /*public void SavePlayerOld() {
            lock (lockStatic) {
                string path = GetPlayerPath(Name);
                FileStream writeStream;
                writeStream = new FileStream(path, FileMode.Create);
                BinaryWriter wbin = new BinaryWriter(writeStream);

                wbin.Write((string)Name);
                wbin.Write((string)Password);
                wbin.Write((byte)Level);
                wbin.Write((uint)Experience);
                wbin.Write((byte)MagicLevel);
                wbin.Write((uint)ManaSpent);
                wbin.Write((ushort)CurrentHP);
                wbin.Write((ushort)MaxHP);
                wbin.Write((ushort)CurrentMana);
                wbin.Write((ushort)MaxMana);
                wbin.Write((ushort)CurrentCapacity);
                wbin.Write((ushort)MaxCapacity);
                wbin.Write((byte)0); //access
                wbin.Write((byte)0); //gender
                wbin.Write((byte)CurrentVocation);
                wbin.Write((ushort)CurrentPosition.x);
                wbin.Write((ushort)CurrentPosition.y);
                wbin.Write((byte)CurrentPosition.z);
                wbin.Write((byte)CharType);
                wbin.Write((byte)OutfitLower);
                wbin.Write((byte)OutfitMiddle);
                wbin.Write((byte)OutfitUpper);
                for (byte i = 0; i < Constants.SKILL_MAX; i++) {
                    wbin.Write((byte)GetSkill(i)); //Skill level
                    wbin.Write((uint)GetSkillTries(i));
                }
                wbin.Write((byte)CurrentDirection);

                for (byte i = 0; i < inventory.Length; i++) {
                    if (inventory[i] == null) {
                        wbin.Write((bool)false); //No item there
                    } else {
                        wbin.Write((bool)true); //Item here
                        inventory[i].SaveItem(wbin);
                    }
                }

                wbin.Write((int)BaseSpeed);

                wbin.Close();
            }
        }*/

        public void AddFishingTry() {
            AddSkillTry(Constants.SKILL_FISHING);
        }
		
		public bool LoadPlayer(LoginInfo info)
		{
			lock (lockStatic)
			{
				string connectString = "URI=file:data/players.db";
				
				IDbConnection dbConnection;
				dbConnection = (IDbConnection) new SqliteConnection(connectString);
				dbConnection.Open();
				
				if (dbConnection == null || dbConnection.State != ConnectionState.Open)
				{
#if DEBUG
					Log.WriteDebug("Player database could not be opened." + dbConnection.State);
#endif
					
					return false;
				}
				
#if DEBUG
				Log.WriteDebug("Player database loaded.");
#endif
				
				IDbCommand dbCommand = dbConnection.CreateCommand();
				
				string query = "SELECT * FROM players WHERE name = '" + info.GetUsername() + "' AND password = '" + info.GetPassword() + "'";
				
				dbCommand.CommandText = query;
				
				IDataReader reader = dbCommand.ExecuteReader();
				
				/*reader["password"]
				
				while (reader.Read())
				{
					Tracer.Println(Convert.ToString(reader.GetValue(21)));
				}*/
				
				if (!reader.Read())
				{
#if DEBUG
					Log.WriteDebug("Player " + info.GetUsername() + " / " + info.GetPassword() + " does not exists!");
#endif
					
					reader.Close();
					reader = null;
					dbCommand.Dispose();
					dbCommand = null;
					dbConnection.Close();
					dbConnection = null;
					
					return false;
				}
				
				Name = reader.GetString(reader.GetOrdinal("name"));
				Password = reader.GetString(reader.GetOrdinal("password"));
				
				Level = reader.GetByte(reader.GetOrdinal("level"));
				Experience = (uint)reader.GetInt32(reader.GetOrdinal("experience"));
				
				MagicLevel = reader.GetByte(reader.GetOrdinal("magic_level"));
				ManaSpent = (uint)reader.GetInt32(reader.GetOrdinal("mana_spent"));
				
				CurrentHP = (ushort)reader.GetInt16(reader.GetOrdinal("current_hp"));
				MaxHP = (ushort)reader.GetInt16(reader.GetOrdinal("max_hp"));
				CurrentMana = (ushort)reader.GetInt16(reader.GetOrdinal("current_mana"));
				MaxMana = (ushort)reader.GetInt16(reader.GetOrdinal("max_mana"));
				CurrentCapacity = (ushort)reader.GetInt16(reader.GetOrdinal("current_cap"));
				MaxCapacity = (ushort)reader.GetInt16(reader.GetOrdinal("max_cap"));
				
				// read access byte
				// read gender byte
				CurrentVocation = (Vocation) (int)reader.GetByte(reader.GetOrdinal("vocation"));
				
				CurrentPosition = new Position();
				CurrentPosition.x = (ushort)reader.GetInt16(reader.GetOrdinal("position_x"));
				CurrentPosition.y = (ushort)reader.GetInt16(reader.GetOrdinal("position_y"));
				CurrentPosition.z = reader.GetByte(reader.GetOrdinal("position_z"));
				CurrentDirection = Direction.SOUTH;
				
				CharType = reader.GetByte(reader.GetOrdinal("char_type"));
				OutfitUpper = reader.GetByte(reader.GetOrdinal("outfit_upper"));
				OutfitMiddle = reader.GetByte(reader.GetOrdinal("outfit_middle"));
				OutfitLower = reader.GetByte(reader.GetOrdinal("outfit_lower"));
				
				SetSkill(Constants.SKILL_FIST, reader.GetByte(reader.GetOrdinal("skill_fist")));
				SetSkill(Constants.SKILL_CLUB, reader.GetByte(reader.GetOrdinal("skill_club")));
				SetSkill(Constants.SKILL_SWORD, reader.GetByte(reader.GetOrdinal("skill_sword")));
				SetSkill(Constants.SKILL_AXE, reader.GetByte(reader.GetOrdinal("skill_axe")));
				SetSkill(Constants.SKILL_DISTANCE, reader.GetByte(reader.GetOrdinal("skill_distance")));
				SetSkill(Constants.SKILL_SHIELDING, reader.GetByte(reader.GetOrdinal("skill_shielding")));
				SetSkill(Constants.SKILL_FISHING, reader.GetByte(reader.GetOrdinal("skill_fishing")));
				
				SetSkillTries(Constants.SKILL_FIST, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_fist")));
				SetSkillTries(Constants.SKILL_CLUB, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_club")));
				SetSkillTries(Constants.SKILL_SWORD, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_sword")));
				SetSkillTries(Constants.SKILL_AXE, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_axe")));
				SetSkillTries(Constants.SKILL_DISTANCE, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_distance")));
				SetSkillTries(Constants.SKILL_SHIELDING, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_shielding")));
				SetSkillTries(Constants.SKILL_FISHING, (uint)reader.GetInt32(reader.GetOrdinal("skill_tries_fishing")));
				
				BaseSpeed = reader.GetInt32(reader.GetOrdinal("base_speed"));
				
				// TODO: ALL ITEMS ON CHARACTER (BACKPACK, DEPOT ETC)
				/*inventory[Constants.INV_HEAD] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_head")));
				inventory[Constants.INV_NECK] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_neck")));
				inventory[Constants.INV_BACKPACK] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_backpack")));
				inventory[Constants.INV_BODY] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_body")));
				inventory[Constants.INV_RIGHT_HAND] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_right_hand")));
				inventory[Constants.INV_LEFT_HAND] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_left_hand")));
				inventory[Constants.INV_LEGS] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_legs")));
				inventory[Constants.INV_FEET] = Item.CreateItem((ushort)reader.GetInt16(reader.GetOrdinal("inventory_feet")));*/
				
				// INVENTORY TODO: Make better woho.
				string invData = reader.GetString(reader.GetOrdinal("inventory"));
				Stream invStream = new MemoryStream(Convert.FromBase64String(invData));
				BinaryReader br = new BinaryReader(invStream);
				
				if (invStream.Length != 0)
				{
					inventory[Constants.INV_NECK] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_HEAD] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_LEFT_HAND] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_RIGHT_HAND] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_LEGS] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_BODY] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_FEET] = Item.CreateItem(br.ReadUInt16());
					inventory[Constants.INV_BACKPACK] = Item.CreateItem(br.ReadUInt16());
				}
				
				// Load backpack items TODO: Doesn't really work, does it? Create method like GetItemsInContainer()
				//if (inventory[Constants.INV_BACKPACK] != null && inventory[Constants.INV_BACKPACK].Type == /*Constants.TYPE_CONTAINER // why doesn't this work?*/ 9220)
				/*{
					Container bp = (Container)inventory[Constants.INV_BACKPACK];
					
					byte bpItemsCount = br.ReadByte();
					
					if (bp.GetItems().Count != 0)
					{
						Tracer.Println("WOHO! LOADING FROM BP(2)");
						for (int i = 0; i < bpItemsCount; i++)
						{
							Item newItem = Item.CreateItem(br.ReadUInt16());
							bp.AddItem(newItem);
							Tracer.Println("newItem=" + newItem.Name + " (" + newItem.ItemID + ")");
							if (newItem.Type == 9220)
							{
								byte bpBpItemsCount = br.ReadByte();
								
								for (int j = 0; j < bpBpItemsCount; j++)
								{
									Item newBpItem = Item.CreateItem(br.ReadUInt16());
									bp.AddItem(newBpItem);
									Tracer.Println("newBpItem=" + newBpItem.Name + " (" + newBpItem.ItemID + ")");
								}
							}
						}
					}
				}*/
				
				reader.Close();
				reader = null;
				dbCommand.Dispose();
				dbCommand = null;
				dbConnection.Close();
				dbConnection = null;
				
				return true;
			}
		}
		
		// TODO: REMOVE
        /*/// <summary>
        /// Returns whether the supplied name and the supplied password are valid.
        /// </summary>
        /// <param name="name">Name of player to be loaded</param>
        /// <param name="password">Password of player to be loaded.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool LoadPlayerOld(LoginInfo info) {
            lock (lockStatic) {
                string filePath = GetPlayerPath(info.GetUsername());
                if (!File.Exists(filePath)) {
#if DEBUG
                    Tracer.Println("Player: " + Name + " does not exist.");
#endif
                    return false;
                }

                BinaryReader bReader = new BinaryReader(File.Open(filePath, FileMode.Open));
                Name = (bReader.ReadString());
                Password = (bReader.ReadString());
                if (!Password.Equals(info.GetPassword())) {
#if DEBUG
                    Tracer.Println("Invalid player password for : " + Name);
#endif
                    bReader.Close();
                    return false;
                }

                Level = (bReader.ReadByte());
                Experience = (bReader.ReadUInt32());
                MagicLevel = (bReader.ReadByte());
                ManaSpent = (bReader.ReadUInt32());
                CurrentHP = (bReader.ReadUInt16());

                MaxHP = (bReader.ReadUInt16());
                CurrentMana = (bReader.ReadUInt16());
                MaxMana = (bReader.ReadUInt16());
                CurrentCapacity = (bReader.ReadUInt16());
                MaxCapacity = (bReader.ReadUInt16());

                bReader.ReadByte(); //access
                bReader.ReadByte(); //gender

                CurrentVocation = (Vocation)(bReader.ReadByte());
                CurrentPosition = new Position();
                CurrentPosition.x = bReader.ReadUInt16();
                CurrentPosition.y = bReader.ReadUInt16();
                CurrentPosition.z = bReader.ReadByte();
                CharType = (bReader.ReadByte());
                OutfitUpper = bReader.ReadByte();
                OutfitMiddle = bReader.ReadByte();
                OutfitLower = bReader.ReadByte();

                for (byte i = 0; i < Constants.SKILL_MAX; i++) {
                    SetSkill(i, bReader.ReadByte());
                    SetSkillTries(i, bReader.ReadUInt32());
                }
                CurrentDirection = (Direction)bReader.ReadByte();
                
                for (byte i = 0; i < inventory.Length; i++) {
                    bool existsItem = bReader.ReadBoolean();
                    if (existsItem) {
                        inventory[i] = Item.Load(bReader);
                    }
                }
				
                BaseSpeed = bReader.ReadInt32();
				
                bReader.Close();
                return true;
            }
        }*/


        /// <summary>
        /// Lets this thing know that a local chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        /// <param name="pos">Position of chat.</param>
        /// <param name="nameFrom">Name of creature doing the chat.</param>
        public override void AddLocalChat(ChatLocal chatType, string message,
            Position pos, Creature creatureFrom) {
            AddAffected();
            string nameFrom = creatureFrom.Name;
            protocolS.AddLocalChat(chatType, message, pos, nameFrom);
        }

        /// <summary>
        /// Lets this thing know that a global chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        /// <param name="nameFrom">Name of creature doing the chat.</param>
        public override void AddGlobalChat(ChatGlobal chatType, string message,
            string nameFrom) {
                AddAffected();
            protocolS.AddGlobalChat(chatType, message, nameFrom);
        }

        /// <summary>
        /// Lets this thing know that an anonymous chat event has occured.
        /// </summary>
        /// <param name="chatType">Type of chat.</param>
        /// <param name="message">Message content.</param>
        public override void AddAnonymousChat(ChatAnonymous chatType, string message) {
            AddAffected();
            protocolS.AddAnonymousChat(chatType, message);
        }
		
		// TODO: REMOVE?
        /*/// <summary>
        /// Returns true whether a player with this name exists, false otherwise.
        /// </summary>
        /// <param name="name">Player name to check.</param>
        /// <returns><True if the player exists, false otherwise/returns>
        public static bool ExistsPlayer(string name) {
            string path = GetPlayerPath(name);
            return File.Exists(path);
        }*/

        /// <summary>
        /// Gets the Inventory index of the item. Note: Item
        /// must be in inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public byte GetInventoryIndex(Item item) {
            for (byte i = 0; i < inventory.Length; i++) {
                if (item == inventory[i]) {
                    return i;
                }
            }
            throw new Exception("Invalid item argument in GetInventoryItem()");
        }

        /// <summary>
        /// Adds an inventory item to the player's inventory and
        /// appends the protocol information.
        /// </summary>
        /// <param name="index">Inventory index to add.</param>
        /// <param name="invItem">Inventory item to add.</param>
        public void AddInventoryItem(byte index, Item invItem) {
            AddAffected();
            inventory[index] = invItem;
            invItem.CarryingInventory = this;
            protocolS.AddInventoryItem(index, invItem);
        }

        /// <summary>
        /// Removes an Inventory item as specified by the index and appends
        /// the protocol data.
        /// </summary>
        /// <param name="index">Inventory index of the item.</param>
        public void RemoveInventoryItem(byte index) {
            AddAffected();
            Item item = inventory[index];
            if (item != null) {
                item.CarryingInventory = null;
            }
            inventory[index] = null;
            protocolS.RemoveInventoryItem(index);
        }

        public override void AddThingToGround(Thing thing,
            Position position, byte stackpos) {
            if (CanSee(position)) {
                thing.AddThisToGround(protocolS, this, position, stackpos);
            }
        }

        /// <summary>
        /// Open the specified container and resets/sends data.
        /// </summary>
        /// <param name="container">The container to open.</param>
        public void OpenContainer(Container container) {
            if (openContainers[0] == container ||
                openContainers[1] == container) {
                return;
            }

            byte index = GetFreeContainerIndex();
            if (openContainers[index] != null) {
                openContainers[index].RemoveViewer(this);
            }

            openContainers[index] = container;
            container.AddViewer(this);
            AddAffected();
            protocolS.AddContainerOpen(index, container);
        }

        /// <summary>
        /// Closes specified container and appends the data.
        /// </summary>
        /// <param name="localID">ID of the container</param>
        public void AppendCloseContainer(byte localID) {
            if (openContainers[localID] == null) {
                return;
            }

            openContainers[localID].RemoveViewer(this);
            openContainers[localID] = null;
            AddAffected();
            protocolS.AddContainerClose(localID);
        }

        /// <summary>
        /// Closes the specified container. Note: Container
        /// must be open.
        /// </summary>
        /// <param name="container">Container to close</param>
        public void AppendCloseContainer(Container container) {
            if (openContainers[0] == container) {
                AppendCloseContainer(0);
            } else if (openContainers[1] == container) {
                AppendCloseContainer(1);
            } else {
                throw new Exception("Invalid call to CloseContainer()");
            }
        }

        public void UpdateCarryingItem(byte index, UpdateCarryingType type, Item newItem) {
            AddAffected();
            protocolS.UpdateCarryingItem(index, type, newItem);
        }

        /// <summary>
        /// Updates the player's client with the specified
        /// container.
        /// </summary>
        /// <param name="container">The container to update.</param>
        public void UpdateContainer(Container container) {
            AddAffected();
            protocolS.UpdateOpenContainer(
                GetContainerIndex(container), container);
        }

        public Container GetContainerByIndex(int ID) {
            if (ID < 0 || ID > Constants.MAX_CONTAINERS) {
                return null;
            }
            return openContainers[ID];
        }

        public override void UpdateItem(Position pos, Item item, byte stackpos) {
            if (CanSee(pos)) {
                AddAffected();
                protocolS.UpdateItem(pos, item, stackpos);
            }
        }

        /// <summary>
        /// Adds the player's stats to the player's client.
        /// </summary>
        public void AddStats() {
            AddAffected();
            protocolS.AddStats(this);
        }

        /// <summary>
        /// Gets whether the player has a specific thing.
        /// </summary>
        /// <param name="item">The thing to test.</param>
        /// <returns>True if the player has the thing, false otherwise.</returns>
        public bool HasSpecificThing(Thing thing) {
            if (thing.CurrentPosition != null) {
                return false;
            }

            for (byte i = 1; i < Constants.INV_MAX; i++) {
                Item invItem = GetInventoryItem(i);
                if (invItem != null && invItem.ContainsItem(thing)) {
                    return true;
                }
            }
            return false;
        }

        public override void HandleMove() {
            foreach (Item item in inventory) {
                if (item == null) {
                    continue;
                }
                item.HandleMove();
            }
            foreach (Container cont in openContainers) {
                if (cont == null) {
                    continue;
                }
                cont.HandleMove();
            }
        }

        /// <summary>
        /// Gets whether this player is next to this thing
        /// or, in the case of containers, any item inside the container.
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public bool IsNextTo(Thing thing) {
            if (thing.CurrentPosition == null) {
                Item item = thing.GetEquipableItem();
                if (item != null && item.Parent != null) {
                    Console.WriteLine("in item get final parent");
                    if (IsNextTo(item.GetFinalParent())) {
                        return true;
                    }
                }
            } else {
                Console.WriteLine("currentpos: " + thing.CurrentPosition);
                if (IsNextTo(thing.CurrentPosition)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets this creature's regular chat type.
        /// </summary>
        /// <returns>Creature's regular chat type.</returns>
        public override ChatLocal GetChatType() {
            return ChatLocal.SAY;
        }

        /// <summary>
        /// Append a status message to the player's client.
        /// </summary>
        /// <param name="msg">The status message to append</param>
        public override void AddStatusMessage(string msg) {
            AddAffected();
            protocolS.AddStatusMessage(msg);
        }

        public override bool AttackableByMonster() {
            return (Access < Constants.ACCESS_GAMEMASTER);
        }

        public override void AddTeleport(Map gameMap) {
            AddAffected();
            protocolS.AddScreenMap(gameMap, this);
        }

        public override int GetSpeed() {
            return BaseSpeed + HastedSpeed;
        }

        public override void UpdateCreatureLight(Creature creature) {
            AddAffected();
            protocolS.UpdateCreatureLight(creature, creature.SpellLightLevel);
        }

        /// <summary>
        /// Returns an error message if this player can not cast the
        /// specified spell. Returns null if the player can.
        /// </summary>
        /// <param name="spell">The spell to test.</param>
        /// <returns>Null if the play can, an error message if the player can't</returns>
        public override string CanCastSpell(Spell spell) {
            string error = null;
            if (MagicLevel < spell.RequiredMLevel) {
                error = "Your magic level is too low";
            } else if (CurrentMana < spell.ManaCost) {
                error = "You don't have enough mana.";
            } else if (!spell.VocationsFor.Contains(CurrentVocation)) {
                error = "Your vocation may not cast this spell.";
            }
            if (error != null) {
                World.AddMagicEffect(MagicEffect.PUFF, CurrentPosition);
            }
            return error;
        }

        public HastedCheck CurHastedCheck {
            get;
            set;
        }

        public void AppendHasted(double hastedFactor, int durationInSeconds) {
            int hastedSpeed = (int)(BaseSpeed * hastedFactor);
            if (hastedSpeed >= HastedSpeed) { //TODO: Fix this
                CurHastedCheck = new HastedCheck();
                CurHastedCheck.CurrentPlayer = this;
                CurHastedCheck.TimeInCS = (durationInSeconds * 100);
                CurHastedCheck.World = World;
                World.AddEventInCS(CurHastedCheck.TimeInCS, CurHastedCheck.PerformCheck);
                HastedSpeed = hastedSpeed;
            }
        }

        public override void NotifyOfSuccessfulCast(Spell spell) {
            CurrentMana = (ushort)(CurrentMana - spell.ManaCost);
            AppendAddManaSpent(spell.ManaCost * Config.GetManaRate());
            if (spell.Rune != null) {
                Item rune = spell.Rune;
                rune.Charges--;
                if (rune.Charges == 0) {
                    World.AppendRemoveItem(rune);
                }
            }
            AddStats();
        }

        /// <summary>
        /// Add experience to this creature.
        /// </summary>
        /// <param name="amt">The amount to add.</param>
        public override void AddExperienceGain(uint amt) {
            Experience += amt;
            CheckForLevelGain();
            AddStats();
        }

        /// <summary>
        /// Removes the specified number of items by their ID 
        /// for the this player and appends protocol data.
        /// Note: The player must have enough items.
        /// </summary>
        /// <param name="ItemID">The item ID to remove</param>
        /// <param name="count">The number of items to remove.</param>
        public void AppendRemoveItemCount( ushort ItemID, int count) {
            if (GetItemCount(ItemID) < count) {
                throw new Exception("Player does not have enough items in AppendRemoveItemCount()");
            }
            for (byte i = 0; i < Constants.INV_MAX; i++) {
                Item item = inventory[i];
                if (item == null) {
                    continue;
                }
                byte curCount = item.Count;
                item.SubtractItemCount(ItemID, ref count);
                byte newCount = item.Count;
                if (curCount != newCount) {
                    RemoveInventoryItem(i);
                    if (item.Count > 0) {
                        AddInventoryItem(i, item);
                    }
                } 
            }
        }

        /// <summary>
        /// Tries to add the specified item to the player's inventory. If 
        /// the item is too heavy, drops to ground. If 
        /// inventory spots are filled, tries to add to container, 
        /// if container is full, drops to ground. Appends to player's
        /// protocol data.
        /// </summary>
        /// <param name="item"></param>
        public void AddCarryingItem(Item item) {
            if (item.GetWeight() > CurrentCapacity) {
                World.AppendAddItem(item, CurrentPosition);
                return;
            }

            if (!IsTwoHanded()) {
                if (inventory[Constants.INV_RIGHT_HAND] == null) {
                    AddInventoryItem(Constants.INV_RIGHT_HAND, item);
                    return;
                } else if (inventory[Constants.INV_LEFT_HAND] == null) {
                    AddInventoryItem(Constants.INV_LEFT_HAND, item);
                    return;
                }
            }

            foreach (Item invItem in inventory) {
                if (invItem == null) {
                    continue;
                }
                //TODO: Fix container overflow bug
                if (invItem.IsOfType(Constants.TYPE_CONTAINER) &&
                    invItem.HasRoom()) {
                        invItem.AddItem(item);
                        return;
                }
            }

            World.AppendAddItem(item, CurrentPosition);
        }

        public int GetItemCount(ushort itemID, byte invIndex) {
            Item item = GetInventoryItem(invIndex);
            if (item != null) {
              return item.GetItemCount(itemID);
            }
            return 0;
        }

        /// <summary>
        /// Gets how many items of the specified ID this player has.
        /// </summary>
        /// <param name="itemID">The item ID to look for.</param>
        /// <returns></returns>
        public int GetItemCount(ushort itemID) {
            int count = 0;
            for (byte i = 0; i < Constants.INV_MAX; i++) {
                count += GetItemCount(itemID, i);
            }
            return count;
        }

        /// <summary>
        /// Given a container, this method returns
        /// at which index the container is relative
        /// to the player's opened container list. 
        /// Note: container must be in open containers.
        /// </summary>
        /// <param name="container">The container whose index
        /// to look for.</param>
        /// <returns>Index of container.</returns>
        public byte GetContainerIndex(Container container) {
            for (byte i = 0; i < Constants.MAX_CONTAINERS; i++) {
                if (openContainers[i] == container) {
                    return i;
                }
            }
            throw new Exception("Invalid state in GetContainerIndex()");
        }

        public void AppendCreateRune(ushort blankID, ushort runeID, byte charges) {
            if (GetItemCount(blankID, Constants.INV_RIGHT_HAND) > 0) {
                Item rightHand = inventory[Constants.INV_RIGHT_HAND];
                rightHand.ItemID = runeID;
                rightHand.Charges = charges;
                World.AppendUpdateItem(rightHand);
            } else if (GetItemCount(blankID, Constants.INV_LEFT_HAND) > 0) {
                Item leftHand = inventory[Constants.INV_LEFT_HAND];
                leftHand.ItemID = runeID;
                leftHand.Charges = charges;
                World.AppendUpdateItem(leftHand);
            }
        }
		
		public List<Item> GetItemsInContainer(Container container)
		{
			List<Item> itemList = new List<Item>();
			
			foreach (Item item in container.GetItems())
			{
				itemList.Add(item);
				
				if (item.Type == /*Constants.TYPE_CONTAINER*/ 9220) // TODO: WHAT IS THIS? Why not TYPE_CONTAINER?
					itemList.AddRange(GetItemsInContainer((Container)item));
			}
			
			return itemList;
		}
    }
	
}
