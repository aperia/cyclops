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
    /// <summary>
    /// This class represents monsters used in game.
    /// </summary>
    public class Monster : Creature {
        private static Dictionary<string, Monster> masterDictionary 
            = masterDictionary = new Dictionary<string, Monster>();

        private int totalDamageDealt = 0;

        //List to keep track of attackers... useful for dividing experience
        //amongst the attackers(shared experience)
        public List<AttackersInformation> attackersInfoList =
            new List<AttackersInformation>();

        private HashSet<Creature> potentialTargets = new HashSet<Creature>();

        private bool ExistsPath(Creature creature) {
            List<byte> dir = new List<byte>();
            Finder.GetPathTo(this, creature.CurrentPosition, dir, 12, false, false);
            return dir.Count > 0;
        }

        private Creature Master {
            get;
            set;
        }

        private bool IsSummon() {
            return Master != null;
        }


        private uint FigureOutExperienceAmt(int dmgDeal, int dmgTotal) {
            double factor = (double)dmgDeal / (double)dmgTotal;
            double gainedXp = Experience * factor;
            return (uint)gainedXp;
        }

        private void AddLoot(Item corpse) {
            if (Loot == null) { //No loot to add
                return;
            }

            Item container = null;
            if (LootContainer != 0) {
                container = Item.CreateItem(LootContainer);
                corpse.AddItem(container);
            }
            //Add loot
            foreach (LootInfo lootInfo in Loot) {
                int lootRandom = (rand.Next(0, Constants.LOOT_CHANCE_MAX))
                    / Constants.LOOT_RATE;
                if (lootRandom < lootInfo.LootChance) {
                    //Add the loot item
                    Item itemToAdd = Item.CreateItem(lootInfo.LootItem);
                    if (lootInfo.Count > 1) {
                        itemToAdd.Count = (byte)(rand.Next(1, lootInfo.Count));
                    }

                    //If it is inside a container in the dead body,
                    //add it to the container
                    if (lootInfo.InContainer) {
                        //if (container.itemList.Count == insideContainer.capacity)
                        //    continue;
                        container.AddItem(itemToAdd);
                    } else { //Otherwise add it to dead body
                        // if (deadBody.itemList.Count == deadBody.capacity)
                        //    continue;
                        corpse.AddItem(itemToAdd);
                    }
                }
            }
        }

        /// <summary>
        /// Used to construct this object.
        /// </summary>
        public Monster() : base() {
        }

        public LootInfo[] Loot {
            get;
            set;
        }

        /// <summary>
        /// Set the master for this monster thus making it
        /// a summoned/convinced creature. To make this monster wild (again),
        /// send null as the argument.
        /// </summary>
        /// <param name="creature">Creature to set as the master.</param>
        public void SetMaster(Creature creature) {
            if (creature != null) {
                creature.AddSummon(this);
            }
            Master = creature;
        }


        public MonsterSpellInfo[] Spells {
            get;
            set;
        }

        public string[] Talk {
            get;
            set;
        }

        public String Article {
            get;
            set;
        }

        public uint Experience {
            get;
            set;
        }

        public ushort SummonCost {
            get;
            set;
        }

        public bool Pushable {
            get;
            set;
        }

        public int Attack {
            get;
            set;
        }

        public int Skill {
            get;
            set;
        }

        public int Defense {
            get;
            set;
        }

        public int Armor {
            get;
            set;
        }

        public bool CanSeeInvisible {
            get;
            set;
        }

        public int Speed {
            get;
            set;
        }

        public ushort LootContainer {
            get;
            set;
        }

        public override int GetSpeed() {
            return Speed;
        }

        public override int GetShieldValue() {
            return CalculateShielding(Defense, Armor);
        }

        public override int GetAtkValue() {
            return CalculateAttack(Attack, Skill, Constants.DAMAGE_FACTOR_NORMAL);
        }

        public override void AddTileCreature(Creature creature, Position position, 
            byte stackpos) {
            if (!creature.AttackableByMonster()) {
                return;
            }
            potentialTargets.Add(creature);
        }

        public override void AddCreatureMove(Direction direction, Creature creature,
            Position oldPos, Position newPos, byte oldStackpos, byte newStackpos) {
            if (!creature.AttackableByMonster()) {
                return;
            }
            potentialTargets.Add(creature);
            PerformThink();
        }

        public override void AddScreenCreature(Creature creature, Position position, 
            byte stackpos) {
            if (!creature.AttackableByMonster()) {
                return;
            }
            potentialTargets.Add(creature);
            PerformThink();
        }

      

        public override void AppendNotifyOfDeath(Item corpse, Map gameMap) {
            AddLoot(corpse);
            gameMap.GetThingsInVicinity(CurrentPosition);
            //Give experience to all creatures who attacked
            foreach (AttackersInformation atkInfo in attackersInfoList) {
                    if (atkInfo.Attacker.LogedIn) {
                        uint xp = FigureOutExperienceAmt(atkInfo.DamageByCreature, totalDamageDealt);
                        atkInfo.Attacker.AddExperienceGain(xp * Config.GetXPRate());
                    }
            }
            if (IsSummon()) {
                Master.RemoveSummon(this);
            }
            World.AppendRemoveMonster(this);
        }

        /// <summary>
        /// Loads all the monsters.
        /// </summary>
        public static void Load() {
            string path = Config.GetDataPath() + Config.GetMonsterDirectory();
            FileInfo[] fileList = new DirectoryInfo(path).GetFiles();
            DynamicCompile dCompile = new DynamicCompile();
            foreach (FileInfo info in fileList) {
                Monster monster = (Monster)dCompile.Compile(path + info.Name, null);
                masterDictionary.Add(monster.Name.ToLower(), monster);
            }
        }

        /// <summary>
        /// Returns the specified monster or null if such a 
        /// monster does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Monster CreateMonster(string name) {
            if (!ExistsMonster(name)) {
                return null;
            }

            return masterDictionary[name.ToLower()].Clone();
        }

        /// <summary>
        /// Checks whether a given monster exists. True if the 
        /// monster exists, false otherwise.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ExistsMonster(string name) {
            return masterDictionary.ContainsKey(name.ToLower());
        }

        /// <summary>
        /// Gets how much a specified monster costs to summon. Note:
        /// Returns 0 if such a monster can't be summoned or if such
        /// a monster doesn't exist.
        /// </summary>
        /// <param name="name">The name of the monster.</param>
        /// <returns>Summon cost or 0.</returns>
        public static ushort GetSummonCost(string name) {
            string lower = name.ToLower();
            if (!masterDictionary.ContainsKey(lower)) {
                return 0;
            }
            return masterDictionary[lower].SummonCost;
        }

        /// <summary>
        /// Gets the specified monster's look type or 0
        /// if such a monster does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static byte GetCharType(string name) {
            if (!masterDictionary.ContainsKey(name.ToLower())) {
                return 0;
            }
            return masterDictionary[name.ToLower()].CharType;
        }

        public Monster Clone() {
            Monster monster = new Monster();
            monster.CurrentHP = CurrentHP;
            monster.Name = Name;
            monster.CharType = CharType;
            monster.Experience = Experience;
            monster.CurrentHP = CurrentHP;
            monster.MaxHP = MaxHP;
            monster.Corpse = Corpse;
            monster.Attack = Attack;
            monster.Skill = Skill;
            monster.Armor = Armor;
            monster.Defense = Defense;
            monster.Talk = Talk; //Shallow copy
            monster.Spells = Spells; //Shallow copy
            monster.Loot = Loot;
            monster.LootContainer = LootContainer;
            monster.Speed = Speed;
            monster.Immunities = Immunities;
            monster.SummonCost = SummonCost;
            monster.Corpse = Corpse;
            monster.MaxSummons = MaxSummons;
            return monster;
        }
        public void PerformThink() {
            if (IsSummon()) {
                Creature cAtking = Master.GetCreatureAttacking();
                if (cAtking == null || !cAtking.LogedIn) {
                    CurrentWalkSettings.Destination = Master.CurrentPosition;
                    CurrentWalkSettings.IntendingToReachDes = false;
                    SetCreatureAttacking(null);
                } else {
                    SetCreatureAttacking(cAtking);
                }
            } else {
                if (GetCreatureAttacking() != null && GetCreatureAttacking().LogedIn
                && ExistsPath(GetCreatureAttacking())) {
                    return;
                }

                //Look for potential targets
                foreach (Creature creature in potentialTargets) {
                    if (!creature.AttackableByMonster() || !creature.LogedIn) {
                        continue;
                    }

                    if (ExistsPath(creature)) {
                        if (FollowCheck == null) {
                            InitCreatureCheck(World);
                        }
                        SetCreatureAttacking(creature);
                        return;
                    }
                }

                StopCreatureCheck();
                SetCreatureAttacking(null); //Do nothing, no creature ;/
            }
        }

        public Respawn CurrentRespawn {
            get;
            set;
        }

        public override bool AttackableByMonster() {
            if (Master != null)
                return Master.AttackableByMonster();
            return false;
        }

        /// <summary>
        /// Gets this creature's regular chat type.
        /// </summary>
        /// <returns>Creature's regular chat type.</returns>
        public override ChatLocal GetChatType() {
            return ChatLocal.ORANGE;
        }

        public override void RemoveThing(Position position, byte stackpos) {
            PerformThink();
        }

        public override string GetTalk() {
            if (GetCreatureAttacking() != null && GetCreatureAttacking().LogedIn &&
                IsNextTo(GetCreatureAttacking().CurrentPosition)) {
                TurnTowardsTarget(GetCreatureAttacking()); //TODO: better code placement
            }

            if (Talk == null) {
                return null;
            }

            foreach (String talk in Talk) {
                //TODO: Finish coding
                if (rand.Next(0, 400) == 5) {
                    return talk;
                }
            }
            return null;
        }

        public override Spell GetSpell() {
            if (Spells == null || GetCreatureAttacking() == null) {
                return null;
            }

            foreach (MonsterSpellInfo info in Spells) {
                if (info.Chance >= rand.Next(0, 400)) {
                    return Spell.CreateCreatureSpell(info.Name, info.Argument,
                        this, info.Min, info.Max, CurrentPosition.Clone());
                }
            }
            return null;
        }

        
        public override string GetLookAt(Player player) {
            //TODO: Handle article
            return base.GetLookAt(player) + Name.ToLower() + ".";
        }

        public override void AddDamage(int dmgAmt, Creature attacker, bool magic) {
            base.AddDamage(dmgAmt, attacker, magic);
            totalDamageDealt += dmgAmt;
            if (attacker == null) {
                return;
            }

            foreach (AttackersInformation info in attackersInfoList) {
                if (info.Attacker == attacker) {
                    info.DamageByCreature += dmgAmt;
                    return;
                }
            }

            //Attacker not found in list... therefore add attacker
            attackersInfoList.Add(new AttackersInformation(attacker, dmgAmt));
        }
    }

    public class MonsterSpellInfo {
        public MonsterSpellInfo(string name, int chance, int min, int max, string arg) {
            Name = name;
            Chance = chance;
            Min = min;
            Max = max;
            Argument = arg;
        }

        public string Name {
            get;
            set;
        }

        public int Chance {
            get;
            set;
        }

        public int Min {
            get;
            set;
        }

        public int Max {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an argument. Only used with
        /// spells that have arguments such as summoning.
        /// </summary>
        public string Argument {
            get;
            set;
        }
    }

    /// <summary>
    /// Used for calculating shared xp.
    /// </summary>
    public class AttackersInformation {

        public AttackersInformation(Creature atker, int amtDealt) {
            Attacker = atker;
            DamageByCreature = amtDealt;
        }

        public Creature Attacker {
            get;
            set;
        }

        public int DamageByCreature {
            get;
            set;
        }
    }


    /// <summary>
    /// Used for monster loot.
    /// </summary>
    public class LootInfo {
        public LootInfo(ushort lootItem, int lootChance,
            bool inContainer, byte count) {
            LootItem = lootItem;
            LootChance = lootChance;
            Count = count;
            InContainer = inContainer;
        }


        public ushort LootItem {
            get;
            set;
        }

        public int LootChance {
            get;
            set;
        }

        public byte Count {
            get;
            set;
        }

        public bool InContainer {
            get;
            set;
        }
    }
}
