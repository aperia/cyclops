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
    /// The creature class, used to initialize all creatures.
    /// </summary>
    public abstract class Creature : Thing {
        private static uint lastID = 101;
        private uint ID;
        protected Random rand = new Random();
        private List<byte> dirList = new List<byte>(); //Direction list
        private List<Monster> summons = new List<Monster>();

        private Creature CreatureAttacking {
            get;
            set;
        }

        /// <summary>
        /// Calculate the attack damage total as if the attacked creature does not have
        /// any armor on.
        /// </summary>
        /// <param name="atk">Attack value of weapon.</param>
        /// <param name="skill">Creature's skill in that field.</param>
        /// <param name="dmgFactor">The damage factor to be used based
        /// on the creature's fight mode.</param>
        /// <returns>a maximum meele formula.</returns>
        protected int CalculateAttack(double atk, double skill, double dmgFactor) {
            //Max meele damage formula
            double maxDamage =
                ((((0.5 * atk * skill)
                + (10 * atk) + (10 * skill)) / 100)
                * dmgFactor);

            int attackAmt = rand.Next(0, (int)maxDamage);


            return attackAmt;
        }

        /// <summary>
        /// Calculate a creature's shielding level.
        /// </summary>
        /// <param name="defense">The creature's defense.</param>
        /// <param name="armor">The creature's armor.</param>
        /// <returns></returns>
        protected int CalculateShielding(double defense, double armor) {
            double shieldingAmt = defense;
            double minArmReduction = (armor * .475);
            double maxArmReduction = 0;
            if (armor > 1) {
                maxArmReduction = (armor * .95) - 1;
            }
            int dmgReduction = rand.Next((int)minArmReduction, (int)maxArmReduction);
            shieldingAmt = shieldingAmt + dmgReduction;

            return (int)shieldingAmt;
        }

        public Creature() {
            CurrentHP = 150;
            MaxHP = 150;
            ID = lastID++;
            Access = Constants.ACCESS_NORMAL;
            CharType = 1;
            CurrentDirection = Direction.SOUTH;
            CurrentHealthStatus = HealthStatus.HEALTHY;
            Name = "None";
            OutfitUpper = 0x88;
            OutfitMiddle = 0x88;
            OutfitLower = 0x35;
            LogedIn = false;
            CurrentWalkSettings = new WalkSettings();
            Corpse = 437; //TODO: Move away from hard coding
            Invisible = false;
            Polymorph = 0;
            AddType(Constants.TYPE_BLOCKING | Constants.TYPE_BLOCKS_AUTO_WALK
                | Constants.TYPE_BLOCKS_MONSTERS);
            CurrentFightMode = FightMode.NORMAL;
            CurrentFightStance = FightStance.CHASE;
            AddType(Constants.TYPE_CREATURE);
           
            LastWalk = new Elapser(0);
        }

        public Elapser LastWalk {
            get;
            set;
        }

        /// <summary>
        /// Gets this creature's regular chat type.
        /// </summary>
        /// <returns>Creature's regular chat type.</returns>
        public abstract ChatLocal GetChatType();

        /// <summary>
        /// Sends the protocol data for adding itself to the ground.
        /// </summary>
        /// <param name="proto">A reference to the protocol.</param>
        /// <param name="player">The player for whom to add this to.</param>
        /// <param name="stackPos">The stack position of this thing.</param>
        public override void AddThisToGround(ProtocolSend proto,
            Player Player, Position pos, byte stackPos) {
            proto.AddScreenCreature(this, Player.KnowsCreature(this),
                pos, stackPos);
        }

        public byte MaxSummons {
            get;
            set;
        }

        public DelayedAction CurrentDelayedAction {
            get;
            set;
        }

        public abstract int GetAtkValue();

        public abstract int GetShieldValue();

        public ushort MaxHP {
            get;
            set;
        }

        public CreatureCheck LightCheck {
            get;
            set;
        }

        public int LightTicks {
            get;
            set;
        }

        public CreatureCheck BurningCheck {
            get;
            set;
        }

        /// <summary>
        /// An array of damages to do.
        /// </summary>
        public int[] Burning {
            get;
            set;
        }

        public Creature GetCreatureAttacking() {
            return CreatureAttacking;
        }

        public bool IsImmune(ImmunityType type) {
            if (Immunities == null) {
                return false;
            }
            return Immunities.Contains(type);
        }

        public CreatureCheck PoisonedCheck {
            get;
            set;
        }

        public int[] Poisoned {
            get;
            set;
        }

        public CreatureCheck ElectrifiedCheck {
            get;
            set;
        }

        public int[] Electrified {
            get;
            set;
        }

        public ushort CurrentHP {
            get;
            set;
        }

        public void SetCreatureAttacking(Creature creature) {
            foreach (Monster summon in summons) {
                summon.SetCreatureAttacking(creature);
            }
            CreatureAttacking = creature;
        }

        public Race CurrentRace {
            get;
            set;
        }

        public bool LogedIn {
            get;
            set;
        }

        public WalkSettings CurrentWalkSettings {
            get;
            set;
        }

        public CreatureCheck PolymorphCheck {
            get;
            set;
        }

        public CreatureCheck InvisibleCheck {
            get;
            set;
        }

        public bool Invisible {
            get;
            set;
        }

        public byte PolymorphCharType {
            get;
            set;
        }

        public ushort Polymorph {
            get;
            set;
        }

        public ImmunityType[] Immunities {
            get;
            set;
        }

        /// <summary>
        /// Gets how much damage to be done to this creature based on
        /// the attacking creature's stats.
        /// </summary>
        /// <param name="attacker">The creature attacking.</param>
        /// <returns>The damage amount if > 0 or PUFF/SPARK constants 
        /// otherwise.</returns>
        public int GetDamageAmt(Creature attacker) {
            int atkValue = attacker.GetAtkValue();
            int shieldValue = this.GetShieldValue();
            int netValue = atkValue - shieldValue;

            if (netValue > 0) {
                return netValue;
            } else if (netValue == 0) {
                return Constants.SPARK;
            } else {
                return Constants.PUFF;
            }
        }

        public List<Monster> GetSummons() {
            return summons;
        }

        public void AddSummon(Monster monster) {
            summons.Add(monster);
        }

        public void RemoveSummon(Monster monster) {
            summons.Remove(monster);
        }

        public virtual void AppendNotifyOfDeath(Item corpse, Map gameMap) {
            if (CurrentHP == 0) {
                CreatureAttacking = null;
            }
        }

        /// <summary>
        /// Lets the creature know that it successfully preformed a 
        /// meele or distance attack.
        /// </summary>
        public virtual void NotifyOfAttack() {
        }

        /// <summary>
        /// Gets the creatures unique identifier.
        /// </summary>
        /// <returns>Creatures unique ID.</returns>
        public uint GetID() {
            return ID;
        }


        /// <summary>
        /// Current health status of the creature.
        /// </summary>
        public HealthStatus CurrentHealthStatus {
            get;
            set;
        }

        /// <summary>
        /// Access of the creature.
        /// </summary>
        public byte Access {
            get;
            set;
        }

        /// <summary>
        /// Direction of the creature.
        /// </summary>
        public Direction CurrentDirection {
            get;
            set;
        }

        public ushort Corpse {
            get;
            set;
        }

        public byte OutfitUpper {
            get;
            set;
        }

        public byte OutfitMiddle {
            get;
            set;
        }

        public byte OutfitLower {
            get;
            set;
        }

        public byte CharType {
            get;
            set;
        }

        public byte SpellLightLevel {
            get;
            set;
        }

        /// <summary>
        /// Gets the current creature light level, essentially
        /// the maximumum of its spell light and any light items
        /// in hands.
        /// TODO: Fix for torches etc.
        /// </summary>
        /// <returns></returns>
        public byte GetLightLevel() {
            return SpellLightLevel;
        }

        public PathFinder Finder {
            get;
            set;
        }

        public int BaseSpeed {
            get;
            set;
        }

        public int HastedSpeed {
            get;
            set;
        }

        public FightMode CurrentFightMode {
            get;
            set;
        }


        public FightStance CurrentFightStance {
            get;
            set;
        }


        /// <summary>
        /// Adds itself to the protocol specified via the parameters.
        /// Only to be used when sending map tiles.
        /// </summary>
        /// <param name="proto">The protocol to add to.</param>
        /// <param name="player">The player for whom the add is being done.</param>
        public override void AddItself(ProtocolSend proto, Player player) {
            bool knowsCreature = player.KnowsCreature(this);
            proto.AddTileCreature(this, knowsCreature);
        }

        /// <summary>
        /// Gets the creature's stackpos type.
        /// </summary>
        /// <returns>Creature's stackpos type.</returns>
        public override StackPosType GetStackPosType() {
            return StackPosType.CREATURE;
        }

        /// <summary>
        /// Returns whether the player is using distance attacks.
        /// </summary>
        /// <returns>True if the player is using distance, false otherwise.</returns>
        public virtual bool UsingDistance() {
            return false;
        }

        /// <summary>
        /// Gets how much ammo the player currently has. Note:
        /// The player must be using distance.
        /// </summary>
        /// <returns>Ammo</returns>
        public virtual byte GetAmmo() {
            return 0;
        }

        /// <summary>
        /// Gets the distance type the player is using,
        /// or EFFECT_NONE if the player doesn't have ammo or
        /// isn't using distance.
        /// </summary>
        /// <returns>Distance type being used.</returns>
        public virtual DistanceType GetDistanceType() {
            return DistanceType.EFFECT_NONE;
        }

        public CreatureCheck AttackCheck {
            get;
            set;
        }
        public CreatureCheck FollowCheck {
            get;
            set;
        }

        public CreatureCheck TalkCheck {
            get;
            set;
        }

        public CreatureCheck SpellCheck {
            get;
            set;
        }


        public byte GetHealthPercentage() {
            return (byte)(CurrentHP * 100 / MaxHP);
        }

        public void InitCreatureCheck(GameWorld world) {
            AttackCheck = new AttackCheck();
            AttackCheck.CurrentCreature = this;
            AttackCheck.TimeInCS = 200; //2 seconds
            AttackCheck.World = world;
            world.AddEventInCS(AttackCheck.TimeInCS, AttackCheck.PerformCheck);

            FollowCheck = new FollowCheck();
            FollowCheck.CurrentCreature = this;
            FollowCheck.TimeInCS = 30; //Change based on certain things
            FollowCheck.World = world;
            world.AddEventInCS(FollowCheck.TimeInCS, FollowCheck.PerformCheck);

            TalkCheck = new TalkCheck();
            TalkCheck.CurrentCreature = this;
            TalkCheck.TimeInCS = 49; //50
            TalkCheck.World = world;
            world.AddEventInCS(TalkCheck.TimeInCS, TalkCheck.PerformCheck);

            SpellCheck = new SpellCheck();
            SpellCheck.CurrentCreature = this;
            SpellCheck.TimeInCS = 51; //50
            SpellCheck.World = world;
            world.AddEventInCS(SpellCheck.TimeInCS, SpellCheck.PerformCheck);
        }

        public void StopCreatureCheck() {
            AttackCheck = null;
            FollowCheck = null;
            TalkCheck = null;
            SpellCheck = null;
        }

        /// <summary>
        /// Gets the creature's next move to its intended destination.
        /// </summary>
        /// <returns></returns>
        public Direction GetNextMove() {
            Position dest = CurrentWalkSettings.Destination;
            if (dest == null) {
                return Direction.NONE;
            }

            if (!CurrentWalkSettings.IntendingToReachDes
                && IsNextTo(dest)) {
                CurrentWalkSettings.Destination = null;
            } else if (CurrentWalkSettings.IntendingToReachDes &&
                CurrentPosition.Equals(dest)) {
                CurrentWalkSettings.Destination = null;
            } else if (ExistsPathToDest()) {
                return (Direction)dirList[0];
            }
             
            return Direction.NONE;
        }

        public GameWorld World {
            get;
            set;
        }

        /// <summary>
        /// Gets whether a path exists to as specified
        /// by the creature's walk settings. Note: This method
        /// also puts next walk position, if path exists, in dirList.
        /// </summary>
        /// <returns></returns>
        public bool ExistsPathToDest() {
            bool reachDest = CurrentWalkSettings.IntendingToReachDes;
            Position dest = CurrentWalkSettings.Destination;
            if (dest == null) {
                return false;
            }
            Finder.GetPathTo(this, dest, dirList, 12, false, reachDest);
            return dirList.Count > 0;
        }

        public virtual int GetSpeed() {
            return 100; //TODO: Finish
        }

        public bool IsDead() {
            return CurrentHP == 0;
        }

        /// <summary>
        /// Gets the next message this creature would
        /// like to say. Note: This is mainly used for 
        /// monsters/npcs to check when they should talk.
        /// </summary>
        /// <returns></returns>
        public virtual string GetTalk() {
            return null;
        }

        /// <summary>
        /// Gets the next spell this creature would
        /// like to cast. Note: This is mainly used for checking
        /// when a monster should cast a spell.
        /// </summary>
        /// <returns></returns>
        public virtual Spell GetSpell() {
            return null;
        }
        /// <summary>
        /// Lets the creature know that it was damaged by another creature.
        /// Note: This method adjusts the damage as needed if hp less than 0 or
        /// greater than max.
        /// </summary>
        /// <param name="dmgAmt">Damage amt to add.</param>
        /// <param name="attacker">The name of the attacker, null if none.</param>
        /// <param name="magic">True if it was a magic attack, false otherwise.</param>
        public virtual void AddDamage(int dmgAmt, Creature attacker, bool magic) {
            if (CurrentHP - dmgAmt < 0) {
                dmgAmt = CurrentHP;
            } else if (CurrentHP - dmgAmt > MaxHP) {
                dmgAmt = -1 * (MaxHP - CurrentHP);
            }

            CurrentHP = (ushort)(CurrentHP - dmgAmt);
            if (dmgAmt > 0) {
                string loseString = "You lose " + dmgAmt + " hitpoint" +
                    (dmgAmt > 1 ? "s" : "") + ".";
                string leftString = " You have " + CurrentHP + " hitpoint" +
                    (CurrentHP > 1 ? "s" : "") + " left.";
                AddStatusMessage(loseString + leftString);
            }

            if (CurrentHP == 0) {
                CurrentHealthStatus = HealthStatus.DEAD;
            } else {
                //THE FORMULA! Lolz
                int divisor = MaxHP / 5;
                CurrentHealthStatus = (HealthStatus)((CurrentHP / divisor) + 1);

                if (CurrentHealthStatus >= HealthStatus.TOTAL_TYPES) {
                    throw new Exception("Invalid health status in AddDamage()");
                }
            }
        }

        public override void AppendHandleDamage(int dmgAmt, Creature attacker, ImmunityType type,
            GameWorld world, bool magic) {
            world.AppendAddDamage(attacker, this, dmgAmt, type, magic);
        }

        public virtual bool AttackableByMonster() {
            return false;
        }

        public virtual void AddStatusMessage(string msg) {
        }

        public void TurnTowardsTarget(Creature target) {
            //basic error checks
            if (target == null)
                return;


            //Here comes the formula :D
            Position oppPos = target.CurrentPosition;
            Position thisPos = this.CurrentPosition;

            Direction directionToTurn = Direction.WEST;

            if (oppPos.x < thisPos.x) {
                directionToTurn = Direction.WEST;
            } else if (oppPos.x > thisPos.x) {
                directionToTurn = Direction.EAST;
            } else if (oppPos.y < thisPos.y) {
                directionToTurn = Direction.NORTH;
            } else if (oppPos.y > thisPos.y) {
                directionToTurn = Direction.SOUTH;
            }

            if (CurrentDirection == directionToTurn) {
                //Already facing this direction...
                return;
            }

            World.HandleChangeDirection(this, directionToTurn);
        }

        public virtual void AddTeleport(Map gameMap) {
        }

        /// <summary>
        /// Gets whether this creature can cast the specified spell.
        /// Returns null if the creature can or an error message if the
        /// creature can not.
        /// </summary>
        /// <param name="spell">The spell to check</param>
        /// <returns>Null if the creature can, an error message otherwise.
        /// </returns>
        public virtual string CanCastSpell(Spell spell) {
            return null;
        }

        /// <summary>
        /// Notify this creature that the spell has been successfully
        /// cast.
        /// </summary>
        /// <param name="spell"></param>
        public virtual void NotifyOfSuccessfulCast(Spell spell) {
        }

        /// <summary>
        /// Add experience to this creature.
        /// </summary>
        /// <param name="amt">The amount to add.</param>
        public virtual void AddExperienceGain(uint amt) {
        }

        /// <summary>
        /// Set light level and update
        /// to creature's around this creature.
        /// </summary>
        public void AppendSpellLightLevel(byte lightLevel, int lightSeconds) {
            if (SpellLightLevel >= lightLevel) {
                return;
            }

            LightTicks = lightSeconds / 5;
            SpellLightLevel = lightLevel;
            World.AppendUpdateLight(this);
            LightCheck = new LightCheck();
            LightCheck.CurrentCreature = this;
            LightCheck.TimeInCS = 500;
            LightCheck.World = World;
            World.AddEventInCS(LightCheck.TimeInCS, LightCheck.PerformCheck);
        }

        /// <summary>
        /// TODO: ADD STATUS MESSAGE.
        /// </summary>
        /// <param name="dmgs"></param>
        /// <param name="timeInCS"></param>
        public void AppendBurning(int[] dmgs, long timeInCS) {
            Burning = dmgs;
            CreatureCheck check = new BurningCheck();
            check.CurrentCreature = this;
            check.World = World;
            check.TimeInCS = timeInCS;
            BurningCheck = check;
            World.AddEventInCS(check.TimeInCS, check.PerformCheck);
        }

        public void AppendPoisoned(int[] dmgs, long timeInCS) {
            Poisoned = dmgs;
            CreatureCheck check = new PoisonCheck();
            check.CurrentCreature = this;
            check.World = World;
            check.TimeInCS = timeInCS;
            PoisonedCheck = check;
            World.AddEventInCS(check.TimeInCS, check.PerformCheck);
        }

        public void AppendElectricuted(int[] dmgs, long timeInCS) {
            Electrified = dmgs;
            CreatureCheck check = new ElectrifiedCheck();
            check.CurrentCreature = this;
            check.World = World;
            check.TimeInCS = timeInCS;
            ElectrifiedCheck = check;
            World.AddEventInCS(check.TimeInCS, check.PerformCheck);
        }

        public override void AppendHandlePush(Player player, Position posFrom, ushort thingID, 
            byte stackpos, Position posTo, byte count, GameWorld world) {
            world.GetMovingSystem().HandlePush(player, posFrom, thingID, stackpos, posTo,
                count, this);

        }
    }
}
