using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyclops {
    //TODO: Make these methods thread safe.

    public class LightCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.LightCheck != this || CurrentCreature.SpellLightLevel == 0) {
                return;
            }
            CurrentCreature.LightTicks--;
            bool update = false;
            if (CurrentCreature.LightTicks == 140) {
                CurrentCreature.SpellLightLevel = 9;
                update = true;
            } else if (CurrentCreature.LightTicks == 75) {
                CurrentCreature.SpellLightLevel = 7;
                update = true;
            } else if (CurrentCreature.LightTicks == 35) {
                CurrentCreature.SpellLightLevel = 4;
                update = true;
            } else if (CurrentCreature.LightTicks == 0) {
                CurrentCreature.SpellLightLevel = 1;
                update = true;
                return;
            }

            if (update && CurrentCreature.SpellLightLevel
                != CurrentCreature.GetLightLevel()) {
                World.SendUpdateLight(CurrentCreature);
            }

            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    };

    public class InvisibleCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                      CurrentCreature.InvisibleCheck != this) {
                return;
            }

            CurrentCreature.Invisible = false;
            CurrentCreature.InvisibleCheck = null;
            World.SendUpdateOutfit(CurrentCreature);
        }
    }

    public class PolyMorphCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                     CurrentCreature.PolymorphCheck != this) {
                return;
            }

            CurrentCreature.Polymorph = 0;
            CurrentCreature.PolymorphCharType = 0;
            CurrentCreature.PolymorphCheck = null;
            World.SendUpdateOutfit(CurrentCreature);
        }
    }

    public class BurningCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.BurningCheck != this) {
                return;
            }

            if (CurrentCreature.Burning == null) {
                CurrentCreature.BurningCheck = null;
                return;
            }

            World.HandleBurningCheck(CurrentCreature);
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class HastedCheck : CreatureCheck {
        public Player CurrentPlayer {
            get;
            set;
        }

        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentPlayer) ||
                CurrentPlayer.CurHastedCheck != this) {
                return;
            }

            CurrentPlayer.HastedSpeed = 0;
        }
    }

    public class ManaShieldCheck : CreatureCheck {
        public Player CurrentPlayer {
            get;
            set;
        }

        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentPlayer) ||
                CurrentPlayer.ManaShieldCheck != this) {
                return;
            }

            CurrentPlayer.ManaShield = false;
            CurrentPlayer.ManaShieldCheck = null;
        }
    }

    public class PoisonCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.PoisonedCheck != this) {
                return;
            }

            if (CurrentCreature.Poisoned == null) {
                CurrentCreature.PoisonedCheck = null;
                return;
            }

            World.HandlePoisonCheck(CurrentCreature);
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class ElectrifiedCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.ElectrifiedCheck != this) {
                return;
            }

            if (CurrentCreature.Electrified == null) {
                CurrentCreature.ElectrifiedCheck = null;
                return;
            }

            World.HandleElectrifiedCheck(CurrentCreature);
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class SpellCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.SpellCheck != this) {
                return;
            }

            World.HandleSpellCheck(CurrentCreature);
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class TalkCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.TalkCheck != this) {
                return;
            }
            World.HandleTalkCheck(CurrentCreature);
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class AttackCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.AttackCheck != this) {
                return;
            }

            World.HandleAttackCheck(CurrentCreature, CurrentCreature.GetCreatureAttacking());
            World.AddEventInCS(TimeInCS, PerformCheck);
        }
    }

    public class FollowCheck : CreatureCheck {
        public override void PerformCheck() {
            if (!World.IsCreatureLogedIn(CurrentCreature) ||
                CurrentCreature.FollowCheck != this) {
                return;
            }

            //TODO: Finish coding
            Creature cAtking = CurrentCreature.GetCreatureAttacking();
            if (cAtking != null) {
                if (CurrentCreature.CurrentFightStance == FightStance.CHASE) {
                    CurrentCreature.CurrentWalkSettings.Destination =  cAtking.CurrentPosition;
                    CurrentCreature.CurrentWalkSettings.IntendingToReachDes = false;
                } else {
                    CurrentCreature.CurrentWalkSettings.Destination = null;
                }
            }

            World.HandleMoveCheck(CurrentCreature);
            World.AddEventInCS(10, PerformCheck);
        }
    }
    /// <summary>
    /// The class used for having an event for constantly checking
    /// creatures such as attacking meele every 2 seconds.
    /// </summary>
    public abstract class CreatureCheck {
        public CreatureCheck() {
        }

        public Creature CurrentCreature {
            get;
            set;
        }

        public GameWorld World {
            get;
            set;
        }

        public long TimeInCS {
            get;
            set;
        }

        public DelayedAction CurrentDelayedAction {
            get;
            set;
        }

        public abstract void PerformCheck();
    }

    public interface DelayedAction {
        void DoAction(GameWorld world);
    }

    public class UseItemAction : DelayedAction {
        private Player player;
        private byte itemType;
        private Position pos;
        private ushort itemID;
        private byte stackpos;

        public UseItemAction(Player player, byte itemType,
            Position pos, ushort itemID, byte stackpos) {
            this.player = player;
            this.itemType = itemType;
            this.pos = pos;
            this.itemID = itemID;
            this.stackpos = stackpos;
        }

        public void DoAction(GameWorld world) {
            world.HandleUseItem(player, itemType, pos, itemID, stackpos);
            player.CurrentDelayedAction = null;
        }
    }

    /*
     * Player player, Position posFrom, ushort thingID,
            byte stackpos, Position posTo, byte count) {
     */
    public class MoveItemDelayed : DelayedAction {
        private Player player;
        private Position posFrom;
        private ushort thingID;
        private byte stackpos;
        private Position posTo;
        private byte count;

        public MoveItemDelayed(Player player, Position posFrom,
            ushort thingID, byte stackpos, Position posTo, byte count) {
            this.player = player;
            this.posFrom = posFrom;
            this.thingID = thingID;
            this.stackpos = stackpos;
            this.posTo = posTo;
            this.count = count;
        }
        public void DoAction(GameWorld world) {
            world.HandlePush(player, posFrom, thingID, stackpos, posTo, count);
            player.CurrentDelayedAction = null;
        }
    }

    public class WalkSettings {
        public WalkSettings() {
        }

        public bool IntendingToReachDes {
            get;
            set;
        }

        public Position Destination {
            get;
            set;
        }
    }
}
