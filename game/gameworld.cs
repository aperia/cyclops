using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cyclops {
    public class GameWorld {
        private Map gameMap;
        private Dictionary<string, Player> playersOnline; //TODO: FIX
        private Dictionary<uint, Creature> creaturesOnline;
        private EventHandler eventHandler; //Used for handling game events
        private ChatSystem chatSystem;
        private MovingSystem movingSystem;
        private PathFinder pathFinder;
        protected Object lockThis;
        private SpellSystem spellSystem;
        private PreparedSet preparedSet = new PreparedSet();


        /// <summary>
        /// Used for handling posion damage, fire damage, etc. Note: Sends protocol data.
        /// Returns the new damages to set.
        /// </summary>
        /// <param name="creature"></param>
        private int[] HandleDamageEffectCheck(Creature creature, int[] dmgs, 
            MagicEffect effect, ImmunityType type) {
                if (!creature.LogedIn || dmgs == null) {
                    return null;
                }

                AddMagicEffect(effect, creature.CurrentPosition);
                int[] newDmgs = null;
                int dmg = dmgs[0];
                if (dmgs.Length > 1) {
                    newDmgs = new int[dmgs.Length - 1];
                    for (int i = 1; i < dmgs.Length; i++) {
                        newDmgs[i - 1] = dmgs[i];
                    }
                }

                AppendAddDamage(null, creature, dmg, type, true);
                SendProtocolMessages();
                return newDmgs;
        }

        private void AppendAddCreature(Creature creature, Position position) {
            position = gameMap.GetFreePosition(position, creature);
            ThingSet tSet = gameMap.GetThingsInVicinity(position);
            AddCachedCreature(creature, position);
            creature.InitCreatureCheck(this);
            byte stackpos = gameMap.GetStackPosition(creature, position);
            foreach (Thing thing in tSet.GetThings()) {
                thing.AddScreenCreature(creature, position, stackpos);
            }
        }

        /// <summary>
        /// Adds creature and sends network message to all affected.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="position"></param>
        private void SendAddCreature(Creature creature, Position position) {
            AppendAddCreature(creature, position);
            SendProtocolMessages();
        }

        /// <summary>
        /// Removes the creature from the game world and only
        /// appends the data to the ThingSet without reseting or sending it.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="tSet">A reference for which things to notify of the
        /// creature's removal.</param>
        private void AppendRemoveCreature(Creature creature) {
            creaturesOnline.Remove(creature.GetID());
            creature.LogedIn = false;
            byte stackpos = gameMap.GetStackPosition(creature, creature.CurrentPosition);
            ThingSet tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
            foreach (Monster summon in creature.GetSummons()) {
                summon.SetMaster(null);
            }
            foreach (Thing thing in tSet.GetThings()) {
                thing.RemoveThing(creature.CurrentPosition, stackpos);
            }
            gameMap.RemoveThing(creature, creature.CurrentPosition);
        }

        /// <summary>
        /// Updates the health status of a creature to all creatures
        /// in vicinity.
        /// </summary>
        /// <param name="creature">Creature whose health status to update.</param>
        /// <param name="tSet">The things to update for.</param>
        private void UpdateHealthStatus(Creature creature, Position position) {
            ThingSet tSet = gameMap.GetThingsInVicinity(position);
            foreach (Thing thing in tSet.GetThings()) {
                thing.UpdateHealthStatus(creature);
            }
        }

        /// <summary>
        /// Removes a creature from the game world and resets/sends the 
        /// data to all players.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        private void SendRemoveCreature(Creature creature) {
            AppendRemoveCreature(creature);
            SendProtocolMessages();
        }

        /// <summary>
        /// Adds a magic effect at the given position.
        /// </summary>
        /// <param name="effect">The effect to add.</param>
        /// <param name="position">Add effect at this position.</param>
        public void AddMagicEffect(MagicEffect effect, Position position) {
            ThingSet tSet = gameMap.GetThingsInVicinity(position);
            foreach (Thing thing in tSet.GetThings()) {
                thing.AddEffect(effect, position);
            }
        }

        private void AddShootEffect(DistanceType type, Position origin,
            Position destination, ThingSet tSet) {
            gameMap.GetThingsInVicinity(origin, tSet);
            gameMap.GetThingsInVicinity(destination, tSet);
            foreach (Thing thing in tSet.GetThings()) {
                thing.AddShootEffect((byte)type, origin, destination);
            }
        }

        public GameWorld(Map map) {
            gameMap = map;
            playersOnline = new Dictionary<string, Player>();
            creaturesOnline = new Dictionary<uint, Creature>();
            eventHandler = new EventHandler();
            eventHandler.World = this;
            eventHandler.Start();
            lockThis = new Object();
            chatSystem = new ChatSystem(map, playersOnline);
            pathFinder = new PathFinder(map);
            movingSystem = new MovingSystem(map, this);
            spellSystem = new SpellSystem(map);
        }


        public virtual void SendAddPlayer(Player player, Position position) {
            lock (lockThis) {
                foreach (KeyValuePair<string, Player> kvp in playersOnline) {
                    kvp.Value.AddStatusMessage(player.Name + " has loged in.");
                }
                //GMs or higher are immune to combat damage
                if (player.Access >= Constants.ACCESS_GAMEMASTER) {
                    player.Immunities = new ImmunityType[] 
                     {ImmunityType.IMMUNE_ELECTRIC, ImmunityType.IMMUNE_FIRE, 
                     ImmunityType.IMMUNE_PHYSICAL, ImmunityType.IMMUNE_POISON};
                }

                SendAddCreature(player, position);
                playersOnline.Add(player.Name.ToLower(), player);
                player.AddLoginBytes(gameMap);
                SendProtocolMessages();
            }
        }

        public void AppendBroadcast(Creature sender, string msg) {
            foreach (KeyValuePair<string, Player> kvp in playersOnline) {
                kvp.Value.AddGlobalChat(ChatGlobal.BROADCAST, msg, sender.Name);
            }
        }

        public virtual void AppendRemoveMonster(Monster monster) {
            lock (lockThis) {
                AppendRemoveCreature(monster);
            }
        }

        public virtual void AppendRemovePlayer(Player player, bool reset) {
            lock (lockThis) {
                if (reset) {
                    AppendRemoveCreature(player);
                    playersOnline.Remove(player.Name.ToLower());
                    player.ResetStats();
                    player.SavePlayer();
                } else {
                    player.SavePlayer();
                    AppendRemoveCreature(player);
                    playersOnline.Remove(player.Name.ToLower());
                }
            }
        }

        public virtual void SendRemovePlayer(Player player, bool reset) {
            lock (lockThis) {
                ThingSet tSet = gameMap.GetThingsInVicinity(player.CurrentPosition);
                AppendRemovePlayer(player, reset);
                SendProtocolMessages();
            }
        }

        public void AppendAddItem(Item item, Position position) {
            lock (lockThis) {
                gameMap.AddThing(item, position);
                byte stackpos = gameMap.GetStackPosition(item, position);
                ThingSet tSet = gameMap.GetThingsInVicinity(position);
                foreach (Thing thing in tSet.GetThings()) {
                    thing.AddItem(item, position, stackpos);
                }
            }
        }


        public virtual void SendAddItem(Item item, Position position) {
            lock (lockThis) {
                AppendAddItem(item, position);
                SendProtocolMessages();
            }
        }

        public virtual void HandleChat(Creature creature, string msg) {
            lock (lockThis) {

                if (Command.IsCommand(msg, creature)) {
                    Command.ExecuteCommand(this, gameMap, msg, creature);
                    return;
                }

                chatSystem.HandleChat(creature, msg);

                if (msg == "test") { //TODO: REMOVE PLX
                    HandleCreatureTarget(creature, creature);
                } else if (msg == "magic") {
                    ((Player)creature).AppendAddManaSpent(10000);
                } else if (msg == "combat") {
                    for (int i = 0; i < 1000; i++) {
                        ((Player)creature).NotifyOfAttack();
                        creature.AddDamage(0, creature, false);
                    }
                } else if (msg == "distance") {
                    ((Player)creature).buggabugga();
                } else if (msg == "shooteffect") {
                    Position orig = creature.CurrentPosition;
                    Position dest = orig.Clone();
                    dest.x += 4;
                    dest.y += 2;
                    creature.AddShootEffect((byte)DistanceType.EFFECT_ENERGY,
                        orig, dest);
                    SendProtocolMessages();
                } else if (msg == "a") {
                    ((Player)creature).add();
                } else if (msg == "b") {
                    ((Player)creature).remove();
				} else if (msg == "!dragon") {
					
                } else if (msg == "monster") {
                    List<Monster> monsterList = new List<Monster>();
                   // m.SetMaster(creature);
                    //SendAddCreature(m, gameMap.GetFreePosition(creature.CurrentPosition, m));
                    //SendProtocolMessages();
                    return;
                    //return;
                } else if (msg == "dead") {
                    ThingSet ttSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
                    AppendAddDamage(creature, creature, creature.CurrentHP, ImmunityType.IMMUNE_PHYSICAL, true);
                } else if (msg == "!up") {
                    Position newPos = creature.CurrentPosition.Clone();
                    newPos.z--; //Z goes down as player goes up :D
                    HandleMove(creature, newPos, creature.CurrentDirection);
                    return;
                } else if (msg == "!down") {
                    Position newPos = creature.CurrentPosition.Clone();
                    newPos.z++;
                    HandleMove(creature, newPos, creature.CurrentDirection);
                    return;
                } else if (msg == "!lighthack") {
                    creature.SpellLightLevel = 10;
                    creature.UpdateCreatureLight(creature);
                } else if (msg == "burn") {
                    creature.Burning = new int[] { 10, 10, 10 };
                    creature.BurningCheck = new BurningCheck();
                    creature.BurningCheck.TimeInCS = 200;
                    creature.BurningCheck.World = this;
                    creature.BurningCheck.CurrentCreature = creature;
                    AddEventInCS(creature.BurningCheck.TimeInCS, creature.BurningCheck.PerformCheck);
                } else if (msg == "position") {
                    creature.AddAnonymousChat(ChatAnonymous.WHITE, "Your position: " +
                        creature.CurrentPosition);
                } else if (msg == "knight") {
                    ((Player)creature).CurrentVocation = Vocation.KNIGHT;
                } else if (msg == "sorcerer") {
                    ((Player)creature).CurrentVocation = Vocation.SORCERER;
                } else if (msg == "druid") {
                    ((Player)creature).CurrentVocation = Vocation.DRUID;
                } else if (msg == "paladin") {
                    ((Player)creature).CurrentVocation = Vocation.PALADIN;
                } else if (msg == "hudini") {
                    creature.CharType++;
                    SendUpdateOutfit(creature);
                    return;
                } else if (msg == "resetoutfit") {
                    creature.CharType = 1;
                    SendUpdateOutfit(creature);
                } else if (msg == "resetoutfit") {
                    creature.CharType = 0;
                    SendUpdateOutfit(creature);
                } else if (msg == "level") {
                    ((Player)creature).AddExperienceGain(1000000);
                } else if (msg.ToLower().StartsWith("item")) {
                    msg = Regex.Split(msg, "\\s+")[1];
                    Item item = Item.CreateItem("sword");
                    item.ItemID = ushort.Parse(msg);
                    AppendAddItem(item, creature.CurrentPosition.Clone());
                } else if (msg == "sd") {
                    Item item = Item.CreateItem(2127);
                    item.Charges = 2;
                    AppendAddItem(item, creature.CurrentPosition.Clone());
                } else if (msg == "vial") {
                    Item item = Item.CreateItem("vial");
                    item.FluidType = Fluids.FLUID_MILK;
                    AppendAddItem(item, creature.CurrentPosition.Clone());
                } else if (msg == "speed") {
                    creature.AddAnonymousChat(ChatAnonymous.WHITE, "Your speed is: " + creature.GetSpeed() + ".");
                }

                if (Spell.IsSpell(msg, SpellType.PLAYER_SAY)) {
                    Spell spell = Spell.CreateSpell(msg, (Player)creature);
                    spellSystem.CastSpell(msg, creature, spell, this);
                }
                SendProtocolMessages();
            }
        }

        public virtual void HandleChangeMode(Player player, FightMode fightMode,
            FightStance fightStance) {
                lock (lockThis) {
                    player.CurrentFightMode = fightMode;
                    player.CurrentFightStance = fightStance;
                }
        }

        public virtual void HandleLookAt(Player player, Position posLookAt) {
            string lookAt = "You see nothing.";

            lock (lockThis) {
                Thing thing = movingSystem.GetThing(player, posLookAt,
                    Constants.STACKPOS_TOP_ITEM, true);
                if (thing != null) {
                    lookAt = thing.GetLookAt(player);
                }
                player.AddAnonymousChat(ChatAnonymous.GREEN, lookAt);
                SendProtocolMessages();
            }
        }

        public virtual void HandleLogout(Player player) {
            lock (lockThis) {
                player.MarkConnectionClosed();
                AppendRemovePlayer(player, false);
                SendProtocolMessages();
            }
        }

        public virtual void HandleExitBattle(Player player) {
            lock (lockThis) {
                player.SetCreatureAttacking(null);
            }
        }

        public virtual void HandleUseItem(Player player, byte itemType,
            Position pos, ushort itemID, byte stackpos) {
            lock (lockThis) {
                Thing thing = movingSystem.GetThing(player, pos, stackpos);
                if (thing == null) {
                    return;
                }
                bool carrying = player.HasSpecificThing(thing);
                bool nextTo = player.IsNextTo(thing);

                if (carrying || nextTo) {
                    thing.UseThing(player, this);
                } else if (!nextTo) {
                    player.CurrentWalkSettings.Destination = pos;
                    player.CurrentWalkSettings.IntendingToReachDes = false;
                    player.CurrentDelayedAction = 
                        new UseItemAction(player, itemType, pos, itemID, stackpos);
                }
                SendProtocolMessages();
            }
        }

        public virtual void HandleUseItemWith(Player player, byte itemType,
             Position pos, ushort itemID, byte stackpos, Position posWith, byte stackposWith) {
                 lock (lockThis) {
                     Thing thing = movingSystem.GetThing(player, pos, stackpos);
                     thing.UseThingWith(player, posWith, this, stackposWith);
                     SendProtocolMessages();
                 }
        }

        public virtual void HandleBounceBack(Player player) {
        }

        /// <summary>
        /// Handle moving from a player.
        /// </summary>
        /// <param name="player">The player moving the thing.</param>
        /// <param name="posFrom">The position where the thing current is.</param>
        /// <param name="thingID">The thing's id.</param>
        /// <param name="stackpos">The thing's stackpos.</param>
        /// <param name="posTo">The new position to place the item.</param>
        /// <param name="count">How much of the thing to move, if applicable.</param>
        public virtual void HandlePush(Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count) {
            lock (lockThis) {
                Thing thing = movingSystem.GetThing(player, posFrom, stackpos);
                if (thing != null) {
                    Console.WriteLine("thing name: " + thing.Name);
                    thing.AppendHandlePush(player, posFrom, thingID, stackpos, posTo, count, this);
                }
                SendProtocolMessages();
            }
        }

        /// <summary>
        /// Have a creature set its target that it will be attacking.
        /// </summary>
        /// <param name="creature">The creature attacking.</param>
        /// <param name="target">The creature who is being attacked.</param>
        public virtual void HandleCreatureTarget(Creature creature, Creature target) {
            if (target == null) {
                throw new Exception("Target is null in HandleCreatureTarget()");
            }

            HandleCreatureTarget(creature, target.GetID());
        }

        /// <summary>
        /// Have a creature set its target that it will be attacking.
        /// </summary>
        /// <param name="creature">The creature attacking.</param>
        /// <param name="targetID">The creature ID's who is being attacked.</param>
        public virtual void HandleCreatureTarget(Creature creature, uint targetID) {
            lock (lockThis) {
                if (targetID == 0 ||
                    !creaturesOnline.ContainsKey(targetID)) {
                    creature.SetCreatureAttacking(null);
                    return;
                }

                creature.SetCreatureAttacking(creaturesOnline[targetID]);
            }
        }

        public virtual void HandleManualWalk(Creature creature, Direction direction) {
            lock (lockThis) {
                creature.CurrentDelayedAction = null;
                creature.CurrentWalkSettings.Destination = null;
                HandleWalk(creature, direction);
            }
        }
        public void HandleMove(Creature creature, Position newPos, Direction direction) {
            HandleMove(creature, newPos, direction, true);
        }

        public void AppendHandleMove(Creature creature, Position newPos, Direction direction, bool validateMove) {
            lock (lockThis) {
                Position oldPos = creature.CurrentPosition;
                Tile oldTile = gameMap.GetTile(oldPos);
                Tile newTile = gameMap.GetTile(newPos);
                if (validateMove) {
                    if (newTile == null || newTile.ContainsType(Constants.TYPE_BLOCKING)) {
                        return;
                    }
                }

                foreach (Thing thing in oldTile.GetThings()) {
                    bool proceed = thing.HandleWalkAction(creature, this, WalkType.WALK_OFF);
                    if (!proceed) {
                        return;
                    }
                }
                foreach (Thing thing in newTile.GetThings()) {
                    bool proceed = thing.HandleWalkAction(creature, this, WalkType.WALK_ON);
                    if (!proceed) {
                        return;
                    }
                }

                if (creature.IsNextTo(newPos)) {
                    //TODO: Finish coding speed
                    int speed = GetGroundSpeed(creature.CurrentPosition);
                    int duration = (100 * 90 /*speed*/) / (creature.GetSpeed());
                    creature.LastWalk.SetTimeInCS((uint)duration);

                    if (!creature.LastWalk.Elapsed()) {
                        return;
                    }

                    Position oldPosClone = oldPos.Clone();
                    if (oldPos.y > newPos.y) {
                        direction = Direction.NORTH;
                        oldPosClone.y--;
                        creature.AddScreenMoveByOne(direction, oldPos, oldPosClone, gameMap);
                    } else if (oldPos.y < newPos.y) {
                        direction = Direction.SOUTH;
                        oldPosClone.y++;
                        creature.AddScreenMoveByOne(direction, oldPos, oldPosClone, gameMap);
                    }
                    if (oldPos.x < newPos.x) {
                        direction = Direction.EAST;
                        oldPosClone.x++;
                        creature.AddScreenMoveByOne(direction, oldPos, oldPosClone, gameMap);
                    } else if (oldPos.x > newPos.x) {
                        direction = Direction.WEST;
                        oldPosClone.x--;
                        creature.AddScreenMoveByOne(direction, oldPos, oldPosClone, gameMap);
                    }
                }
                ThingSet tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
                byte oldStackPos = gameMap.GetStackPosition(creature, oldPos);
                gameMap.MoveThing(creature, oldPos, newPos);
                creature.CurrentDirection = direction;
                byte newStackPos = gameMap.GetStackPosition(creature, newPos);
                gameMap.GetThingsInVicinity(newPos, tSet);
                creature.HandleMove();

                foreach (Thing thing in tSet.GetThings()) {
                    thing.AddCreatureMove(direction, creature, oldPos, newPos,
                        oldStackPos, newStackPos);
                }

                if (!creature.IsNextTo(oldPos)) {
                    creature.AddTeleport(gameMap);
                }
            }
        }


        /// <summary>
        /// Sends protocol messages.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="newPos"></param>
        /// <param name="direction"></param>
        public void HandleMove(Creature creature, Position newPos, Direction direction, bool validateMove) {
            lock (lockThis) {
                AppendHandleMove(creature, newPos, direction, validateMove);
                SendProtocolMessages();
            }
        }

        public virtual void HandleWalk(Creature creature, Direction direction) {
            lock (lockThis) {
                Position newPos = Position.GetNewPosition(creature.CurrentPosition, direction);
                HandleMove(creature, newPos, direction);
            }
        }

        public virtual void HandleChangeDirection(Creature creature, Direction direction) {
            lock (lockThis) {
                creature.CurrentDirection = direction;
                ThingSet tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
                byte stackpos = gameMap.GetStackPosition(creature, creature.CurrentPosition);

                foreach (Thing thing in tSet.GetThings()) {
                    thing.UpdateDirection(direction, creature, stackpos);
                }

                SendProtocolMessages();
            }
        }

        public virtual void HandleRequestOutfit(Creature creature) {

        }


        public virtual void HandleMoveCheck(Creature creature) {
            lock (lockThis) {
                Direction dir = creature.GetNextMove();
                if (dir != Direction.NONE) {
                    HandleWalk(creature, dir);
                } else if (creature.CurrentDelayedAction != null) {
                    creature.CurrentDelayedAction.DoAction(this);
                }
            }
        }

        public void HandleTalkCheck(Creature talker) {
            lock (lockThis) {
                string msg = talker.GetTalk();
                if (msg != null) {
                    HandleChat(talker, msg);
                }
            }
        }

        public void HandleSpellCheck(Creature caster) {
            lock (lockThis) {
                Spell spell = caster.GetSpell();
                if (spell != null) {
                    ThingSet tSet = new ThingSet();
                    spellSystem.CastSpell(spell.Name, caster, spell, this);
                    SendProtocolMessages();
                }
            }
        }

        public void HandleBurningCheck(Creature creature) {
            lock (lockThis) {
                int[] newDmg = HandleDamageEffectCheck(creature, creature.Burning, 
                    MagicEffect.BURNED, ImmunityType.IMMUNE_FIRE);
                creature.Burning = newDmg;
            }
        }

        public void HandlePoisonCheck(Creature creature) {
            lock (lockThis) {
                int[] newDmg = HandleDamageEffectCheck(creature, creature.Poisoned,
                    MagicEffect.POISEN_RINGS, ImmunityType.IMMUNE_POISON);
                creature.Poisoned = newDmg;
            }
        }

        public void HandleElectrifiedCheck(Creature creature) {
            lock (lockThis) {
                int[] newDmg = HandleDamageEffectCheck(creature, creature.Electrified,
                    MagicEffect.ENERGY_DAMAGE, ImmunityType.IMMUNE_ELECTRIC);
                creature.Electrified = newDmg;
            }
        }

        public void AppendAddDamage(Creature attacker, Creature attacked,
            int dmg, ImmunityType type, bool magic) {
            lock (lockThis) {
                if (attacked.IsImmune(type)) {
                    AddMagicEffect(MagicEffect.PUFF, attacked.CurrentPosition);
                    return;
                }
                
                if (magic && (attacker is Player)) {
                    dmg = dmg / 2;
                }

                HealthStatus healthStatus = attacked.CurrentHealthStatus;
                Position pos = attacked.CurrentPosition;
   
                attacked.AddDamage(dmg, attacker, magic);
                if (!magic) {
                    attacker.NotifyOfAttack();
                }
                if (attacked.IsDead()) {
                    Item corpse = Item.CreateItem(attacked.Corpse);
                    AppendAddItem(corpse, attacked.CurrentPosition);
                    attacked.AppendNotifyOfDeath(corpse, gameMap);
                }

                if (healthStatus != attacked.CurrentHealthStatus) {
                    UpdateHealthStatus(attacked, pos);
                }
            }
        }

        public virtual void HandleRequestOutfit(Player player) {
            lock (lockThis) {
                player.AddRequestOutfit();
                SendProtocolMessages();
            }
        }

        public void AppendUpdateOutfit(Creature creature) {
            lock (lockThis) {
                ThingSet tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
                foreach (Thing thing in tSet.GetThings()) {
                    thing.UpdateOutfit(creature);
                }
            }
        }

        public void SendUpdateOutfit(Creature creature) {
            lock (lockThis) {
                AppendUpdateOutfit(creature);
                SendProtocolMessages();
            }
        }

        public void HandleSetOutfit(Creature creature, 
            byte lower, byte middle, byte upper) {
            lock (lockThis) {
                creature.OutfitUpper = upper;
                creature.OutfitMiddle = middle;
                creature.OutfitLower = lower;
                SendUpdateOutfit(creature);
            }
        }

        //One huge ugly method. TODO: Split it up.
        public virtual void HandleAttackCheck(Creature attacker, Creature attacked) {
            lock (lockThis) {
                if (attacker == attacked) {
                    //throw new Exception("Attacker == attacked in HandleAttackCheck()");
                    return;
                }

                if (attacked == null || !attacked.LogedIn) {
                    attacker.SetCreatureAttacking(null);
                    return;
                }

                if (attacked.CurrentPosition == null) {
                    throw new Exception("Invalid condition in HandleAttackCheck()");
                }

                if (attacked.CurrentHP == 0) {
                    throw new Exception("Attacked is already dead in HandleAttack()");
                }

                bool usingDistance = attacker.UsingDistance();
                DistanceType shootEffect = DistanceType.EFFECT_NONE;
                if (usingDistance) {
                    if (attacker.GetAmmo() == 0) {
                        return;
                    } else {
                        shootEffect = attacker.GetDistanceType();
                    }
                } else if (!usingDistance &&
                    !attacker.IsNextTo(attacked.CurrentPosition)) {
                    return;
                }

                ThingSet tSet = gameMap.GetThingsInVicinity(attacker.CurrentPosition);
                gameMap.GetThingsInVicinity(attacked.CurrentPosition, tSet);

                if (shootEffect != DistanceType.EFFECT_NONE) {
                    AddShootEffect(shootEffect, attacker.CurrentPosition,
                        attacked.CurrentPosition, tSet);
                }

                int dmg = attacked.GetDamageAmt(attacker);
                if (dmg == Constants.PUFF) {
                    AddMagicEffect(MagicEffect.PUFF, attacked.CurrentPosition);
                    dmg = 0;
                } else if (dmg == Constants.SPARK) {
                    AddMagicEffect(MagicEffect.BLOCKHIT, attacked.CurrentPosition);
                    dmg = 0;
                } else {
                    AddMagicEffect(MagicEffect.DRAW_BLOOD, attacked.CurrentPosition);
                }
                AppendAddDamage(attacker, attacked, dmg, ImmunityType.IMMUNE_PHYSICAL, false);
                SendProtocolMessages();
            }
        }

        public void AppendAddMonster(Monster monster, Position position) {
            lock (lockThis) {
                AppendAddCreature(monster, position);
                AddMagicEffect(MagicEffect.BLUEBALL, monster.CurrentPosition);
                monster.PerformThink();
            }
        }

        public void SendAddMonster(Monster monster, Position position) {
            lock (lockThis) {
                SendAddCreature(monster, position);
            }
        }

        public void SendAddNPC(NPC npc, Position position) {
            lock (lockThis) {
                SendAddCreature(npc, position);
            }
        }

        /// <summary>
        /// Checks whether the given player is online.
        /// </summary>
        /// <param name="player">Player name to check.</param>
        /// <returns>True if the given player is online, false otherwise.</returns>
        public bool IsPlayerOnline(string player) {
            lock (lockThis) {
                return playersOnline.ContainsKey(player.ToLower());
            }
        }

        public Player GetPlayer(string playerName) {
            lock (lockThis) {
                if (!playersOnline.ContainsKey(playerName.ToLower())) {
                    return null;
                }
                return playersOnline[playerName.ToLower()];
            }
        }

        /// <summary>
        /// Add an event to the event handler in centiseconds.
        /// </summary>
        /// <param name="time">Time in centiseconds to have the event called.</param>
        /// <param name="eventDel">The event delegate to perform
        /// after time has passed.</param>
        public void AddEventInCS(long time, eventDelegate eventDel) {
            lock (lockThis) {
                eventHandler.addEventInCS(time, eventDel);
            }
        }

        /// <summary>
        /// Checks whether a creature is loged in, in a 
        /// thread safe manner.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>True if the creature is loged in, false otherwise.</returns>
        public bool IsCreatureLogedIn(Creature creature) {
            lock (lockThis) {
                return creature.LogedIn;
            }
        }

        public virtual void HandleCloseContainer(Player player, byte localID) {
            lock (lockThis) {
                player.AppendCloseContainer(localID);
                SendProtocolMessages();
            }
        }

        public virtual void HandleAutoWalk(Player player, Position pos) {
            lock (lockThis) {
                player.CurrentWalkSettings.Destination = pos;
                player.CurrentWalkSettings.IntendingToReachDes = true;
            }
        }

        /// <summary>
        /// Gets the ground speed at the specified position. Note: This
        /// method is not thread safe.
        /// Note: The position must be valid in terms of the map.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual int GetGroundSpeed(Position position) {
            Tile tile = gameMap.GetTile(position);
            Item item = (Item) tile.GetThings()[0]; //TODO: Fix downcast
            return item.Speed;
        }

        public void AddAffectedPlayer(Player player) {
            preparedSet.AddPlayer(player);
        }

        /// <summary>
        /// Send protocol messages to all affected players
        /// in this game world. Note: This method is not thread-safe.
        /// </summary>
        public void SendProtocolMessages() {
            foreach (Player player in preparedSet.GetThings()) {
                player.WriteToSocket();
                player.ResetNetMessages();
            }
            preparedSet.Clear();
        }

        /// <summary>
        /// Remove the specified item in a thread-safe manner.
        /// </summary>
        /// <param name="itemToRemove"></param>
        public void AppendRemoveItem(Item itemToRemove) {
            lock (lockThis) {
                if (itemToRemove.CarryingInventory != null) {
                    Player player = itemToRemove.CarryingInventory;
                    byte index = player.GetInventoryIndex(itemToRemove);
                    player.RemoveInventoryItem(index);
                } else if (itemToRemove.CurrentPosition != null) {
                    Position pos = itemToRemove.CurrentPosition;
                    Tile mapTile = gameMap.GetTile(pos);
                    byte stackpos = mapTile.GetStackPosition(itemToRemove);
                    mapTile.RemoveThing(itemToRemove);
                    ThingSet tSet = gameMap.GetThingsInVicinity(pos);
                    foreach (Thing thing in tSet.GetThings()) {
                        thing.RemoveThing(pos, stackpos);
                    }
                } else if (itemToRemove.CurrentPosition == null) { //In container
                    Container container = itemToRemove.Parent;
                    container.RemoveItem(itemToRemove);
                }
            }
        }

        /// <summary>
        /// Updates the specified item in a thread-safe manner.
        /// </summary>
        /// <param name="oldItem"></param>
        /// <param name="newItem"></param>
        public void AppendUpdateItem(Item itemToUpdate) {
            lock (lockThis) {
                //Add new attributes to the item w/ the ID
                Item.CreateItem(itemToUpdate.ItemID).Clone(itemToUpdate);
                if (itemToUpdate.CarryingInventory != null) {
                    Player player = itemToUpdate.CarryingInventory;
                    byte index = player.GetInventoryIndex(itemToUpdate);
                    player.RemoveInventoryItem(index);
                    player.AddInventoryItem(index, itemToUpdate);
                } else if (itemToUpdate.CurrentPosition != null) {
                    ThingSet tSet = gameMap.GetThingsInVicinity(itemToUpdate.CurrentPosition);
                    byte stackpos = 
                        gameMap.GetStackPosition(itemToUpdate, itemToUpdate.CurrentPosition);
                    foreach (Thing thing in tSet.GetThings()) {
                        thing.UpdateItem(itemToUpdate.CurrentPosition, itemToUpdate, stackpos);
                    }
                } else if (itemToUpdate.CurrentPosition == null) { //In container
                    Container container = itemToUpdate.Parent;
                    container.UpdateItem(itemToUpdate);
                }
            }
        }

        /// <summary>
        /// Invokes an event in a thread safe manner.
        /// </summary>
        /// <param name="del"></param>
        public void InvokeEvent(eventDelegate del) {
            lock (lockThis) {
                del();
            }
        }

        /// <summary>
        /// Adds a creature to a single tile with only the 
        /// most basic information. Note: Used for speeding up
        /// loading spawns.
        /// </summary>
        /// <param name="creature">Creature to add.</param>
        /// <param name="position">Position to add creature.</param>
        public void AddCachedCreature(Creature creature, Position position) {
            lock (lockThis) {
                creature.World = this;
                creaturesOnline.Add(creature.GetID(), creature);
                creature.Finder = pathFinder;                
                creature.LogedIn = true;
                gameMap.AddThing(creature, position);
            }
        }

        public void AppendUpdateLight(Creature creature) {
            lock (lockThis) {
                ThingSet tSet = gameMap.GetThingsInVicinity(creature.CurrentPosition);
                foreach (Thing thing in tSet.GetThings()) {
                    thing.UpdateCreatureLight(creature);
                }
            }
        }

        public void SendUpdateLight(Creature creature) {
            lock (lockThis) {
                AppendUpdateLight(creature);
                SendProtocolMessages();
            }
        }

        public SpellSystem GetSpellSystem() {
            return spellSystem;
        }

        public Map GetGameMap() {
            return gameMap;
        }

        public MovingSystem GetMovingSystem() {
            return movingSystem;
        }
    }
}
