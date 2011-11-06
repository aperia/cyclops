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
    /// This class contains all of the global constants
    /// in use by this program.
    /// </summary>
    public class Constants {

        public const int MAX_CONTAINERS = 2;

        public const byte DEATH_RATE = 10; //10% death rate as default

        /* Inventory constants */
        public const byte INV_HEAD = 1;
        public const byte INV_NECK = 2;
        public const byte INV_BACKPACK = 3;
        public const byte INV_BODY = 4;
        public const byte INV_RIGHT_HAND = 5;
        public const byte INV_LEFT_HAND = 6;
        public const byte INV_LEGS = 7;
        public const byte INV_FEET = 8;
        //Number of inventory items
        public const int INV_MAX = 9;

        /* Unused in tibia 6.4 */
        public const byte INV_RING = 9;
        public const byte INV_ARROWS = 10;
        public const byte INV_TWO_HAND = 11;
        /* Unused end */

        public const short Z_UP = -1; //inverse because z goes down as u go up
        public const short Z_DOWN = 1; //inverse because z goes up as u go down
        public const ushort MAP_MAX_LAYERS = 16;

        public const int LOOT_CHANCE_MAX = 100000;

        /* Game multipliers */
        public const byte LOOT_RATE = 1;
        public const byte EXPERIENCE_MULTIPLIER = 250;
        public const byte SKILL_MULTIPLIER = 200;
        public const byte MAGIC_LEVEL_MULTIPLIER = 1;

        /* Indexes of the skills array in player */
        public const byte SKILL_FIST = 0;
        public const byte SKILL_CLUB = 1;
        public const byte SKILL_SWORD = 2;
        public const byte SKILL_AXE = 3;
        public const byte SKILL_DISTANCE = 4;
        public const byte SKILL_SHIELDING = 5;
        public const byte SKILL_FISHING = 6;
        public const byte SKILL_MAX = 7;

        /* Item types */
        public const uint TYPE_BLOCKS_ITEM = 0x01;
        public const uint TYPE_ON_TOP = 0x02;
        public const uint TYPE_CONTAINER = 0x04;
        public const uint TYPE_STACKABLE = 0x08;
        public const uint TYPE_USEABLE_WITH = 0x10;
        public const uint TYPE_PILE_UP = 0x20;
        public const uint TYPE_WRITEABLE_EDIT = 0x40;
        public const uint TYPE_WRITEABLE_NO_EDIT = 0x80;
        public const uint TYPE_FLUID_CONTAINER = 0x100;
        public const uint TYPE_BLOCKING = 0x0200; //Blocks
        public const uint TYPE_MOVEABLE = 0x400;
        public const uint TYPE_BLOCKS_PROJECTILES = 0x800;
        public const uint TYPE_BLOCKS_MONSTERS = 0x1000;
        public const uint TYPE_EQUIPABLE = 0x2000;
        public const uint TYPE_LIGHT_ITEMS = 0x4000;
        public const uint TYPE_CAN_SEE_UNDER = 0x8000;
        public const uint TYPE_BLOCKS_MAGIC = 0x10000;
        public const uint TYPE_BLOCKS_AUTO_WALK = 0x20000;
        public const uint TYPE_ITEM = 0x40000;
        public const uint TYPE_CREATURE = 0x80000;

        public const int DAMAGE_FACTOR_NORMAL = 7;

        public const byte CONTAINER_INVALID = 2;

        public const int RANDOM_LIMIT = 400;

        public const byte STACKPOS_TOP_ITEM = 0xFF;

        public const int SPARK = 0;
        public const int PUFF = -1;

        /* Access levels */
        public const byte ACCESS_NORMAL = 1;
        public const byte ACCESS_NPC = 2;
        public const byte ACCESS_GAMEMASTER = 3;
        public const byte ACCESS_ADMIN = 4;

        public const string ACT_MANAGER_NAME = "Account Manager";

        /* Attributes used in items */
        public const string ATTRIBUTE_WEIGHT = "weight";
        public const string ATTRIBUTE_DEFENSE = "defense";
        public const string ATTRIBUTE_ATTACK = "attack";
        public const string ATTRIBUTE_ARMOR = "armor";
        public const string ATTRIBUTE_WEAPON_TYPE = "weaponType";
        public const string ATTRIBUTE_SLOT_TYPE = "slotType";
        public const string ATTRIBUTE_RUNES_SPELL_NAME = "runeSpellName";
        public const string ATTRIBUTE_DESCRIPTION = "description";
        public const string ATTRIBUTE_CHARGES = "charges";
        public const string ATTRIBUTE_SHOW_CHARGES = "showcharges";
        public const string ATTRIBUTE_ABSORB_PERCENT_MANA_DRAIN = "absorbPercentManaDrain";
        public const string ATTRIBUTE_STOP_DURATION = "stopduration";
        public const string ATTRIBUTE_SHOW_DURATION = "showduration";
        public const string ATTRIBUTE_TRANSFORM_EQUIP_TO = "transformEquipTo";
        public const string ATTRIBUTE_DECAY_TO = "decayTo";
        public const string ATTRIBUTE_DURATION = "duration";
        public const string ATTRIBUTE_CONTAINER_SIZE = "containerSize";
        public const string ATTRIBUTE_SHOOT_TYPE = "shootType";
        public const string ATTRIBUTE_AMMO_TYPE = "ammoType";

        public const string WEAPON_TYPE_SWORD = "sword";
        public const string WEAPON_TYPE_AXE = "axe";
        public const string WEAPON_TYPE_DISTANCE = "distance";
        public const string WEAPON_TYPE_CLUB = "club";
        public const string WEAPON_TYPE_SHIELD = "shield";
        public const string WEAPON_TYPE_AMMUNITION = "ammunition";

        public const string SHOOT_TYPE_SMALL_STONE = "smallstone";
        public const string SHOOT_TYPE_SNOW_BALL = "snowball";
        public const string SHOOT_TYPE_SPEAR = "spear";
        public const string SHOOT_TYPE_THROWING_STAR = "throwingstar";
        public const string SHOOT_TYPE_THROWING_KNIFE = "throwingknife";
        public const string SHOOT_TYPE_ENERGY = "energy";
        public const string SHOOT_TYPE_BOLT = "bolt";
        public const string SHOOT_TYPE_ARROW = "arrow";
        public const string SHOOT_TYPE_POISON_ARROW = "poisonarrow";
        public const string SHOOT_TYPE_BURST_ARROW = "burstarrow";
        public const string SHOOT_TYPE_POWER_BOLT = "powerbolt";

        public const byte TEXTCOLOR_BLUE = 5;
        public const byte TEXTCOLOR_LIGHTBLUE = 35;
        public const byte TEXTCOLOR_LIGHTGREEN = 30;
        public const byte TEXTCOLOR_PURPLE = 83;
        public const byte TEXTCOLOR_LIGHTGREY = 129;
        public const byte TEXTCOLOR_DARKRED = 144;
        public const byte TEXTCOLOR_RED = 180;
        public const byte TEXTCOLOR_ORANGE = 198;
        public const byte TEXTCOLOR_YELLOW = 210;
        public const byte TEXTCOLOR_WHITE_EXP = 215;
        public const byte TEXTCOLOR_NONE = 255;

        public const byte SPEAK_NORMAL = 0x01;
        public const byte SPEAK_WHISPER = 0x02;
        public const byte SPEAK_YELL = 0x03;
        public const byte SPEAK_PRIVATE_MSG = 0x04;
        public const byte SPEAK_CHANNEL_YELLOW = 0x05;
        public const byte SPEAK_RV_INIT = 0x06;
        public const byte SPEAK_RV_COUNSELLOR = 0x07;
        public const byte SPEAK_RV_REPORTER = 0x08;
        public const byte SPEAK_BROADCAST = 0x09;
        public const byte SPEAK_MONSTER = 0x0E;
        
        public const byte MSG_SMALLINFO = 0x14;
        public const byte MSG_GREEN = 0x13;
        public const byte MSG_WHITE = 0x11;
        public const byte MSG_IMPORTANT_WHITE = 0x10;
        public const byte MSG_RED = 0x0A;

        public const string AMMO_TYPE_BOLT = "bolt";
        public const string AMMO_TYPE_ARROW = "arrow";

        public const string SLOT_TYPE_TWO_HANDED = "two-handed";
        public const string SLOT_TYPE_BACKPACK = "backpack";
        public const string SLOT_TYPE_RING = "ring";
        public const string SLOT_TYPE_NECKLACE = "necklace";
        public const string SLOT_TYPE_FEET = "feet";
        public const string SLOT_TYPE_LEGS = "legs";
        public const string SLOT_TYPE_BODY = "body";
        public const string SLOT_TYPE_HEAD = "head";

        public const byte ITEM_TYPE_USE = 1;
        public const byte ITEM_TYPE_USE_WITH = 2;
    }

    /*
     *         public const byte RACE_NONE = 0;
        public const byte RACE_VENOM = 1;
        public const byte RACE_BLOOD = 2;
        public const byte RACE_UNDEAD = 3;
        public const byte RACE_FIRE = 4;
    */

    public enum Race : byte {
        VENOM = 1,
        BLOOD = 2,
        UNDEAD = 3,
        FIRE = 4
    }

    public enum FightMode : byte {
        OFFENSIVE = 1,
        NORMAL = 2,
        DEFENSIVE = 3
    }

    public enum FightStance : byte {
        STAND_STILL = 0,
        CHASE = 1,
        KEEP_DISTANCE = 2
    }

    public enum Vocation {
        NONE = 0,
        KNIGHT = 1,
        PALADIN = 2,
        SORCERER = 3,
        DRUID = 4,
        TOTAL = 5
    }

    public enum Direction : byte {
        NORTH = 0,
        WEST = 3,
        SOUTH = 2,
        EAST = 1,
        NONE = 0xFF
    }
    
    public enum ChatLocal {
        ORANGE = 0x41,
        SAY = 0x53,
        WHISPER = 0x57,
        YELLS = 0x59
    }

    public enum ChatGlobal {
        PRIVATE_MSG = 0x50,
        BROADCAST = 0x42
    }

    public enum ChatAnonymous {
        WHITE = 0x47,
        GREEN = 0x4D,
    }

    public enum MagicEffect {
        DRAW_BLOOD = 0,
        LOOSE_ENERGY = 1,
        PUFF = 2,
        BLOCKHIT = 3,
        EXPLOSION_AREA = 4,
        EXPLOSION_DAMAGE = 5,
        FIRE_AREA = 6,
        YELLOW_RINGS = 7,
        POISEN_RINGS = 8,
        HIT_AREA = 9,
        BLUEBALL = 10,
        ENERGY_DAMAGE = 11,
        BLUE_SPARKLES = 12,
        RED_SPARKLES = 13,
        GREEN_SPARKLES = 14,
        BURNED = 15,
        SPLASH_POISION = 16,
        MORT_AREA = 17
    }

    public enum HealthStatus {
        DEAD = 0,
        NEARLY_DEAD = 1,
        CRITICAL = 2,
        HEAVILY_WOUNDED = 3,
        LIGHTLY_WOUNDED = 4,
        BARELY_WOUNDED = 5,
        HEALTHY = 6,
        TOTAL_TYPES = 7
    }

    public enum ImmunityType {
        IMMUNE_PHYSICAL,
        IMMUNE_POISON,
        IMMUNE_FIRE,
        IMMUNE_ELECTRIC,
    };

    public enum DistanceType {
        EFFECT_SPEAR = 0,
        EFFECT_BOLT = 1,
        EFFECT_ARROW = 2,
        EFFECT_FIREBALL = 3,
        EFFECT_ENERGY = 4,
        EFFECT_POISON_ARROW = 5,
        EFFECT_BURST_ARROW = 6,
        EFFECT_THROWING_STAR = 7,
        EFFECT_THROWING_KNIFE = 8,
        EFFECT_SMALL_STONE = 9,
        EFFECT_BLACK_ENERGY = 10, //as in SD
        EFFECT_BIG_STONE = 11,
        EFFECT_POWER_BOLT = 13,
        EFFECT_NONE
    }

    public enum StackPosType {
        GROUND = 0,
        TOP_ITEM = 1,
        CREATURE = 2,
        REGULAR_ITEM = 3
    }

    public enum SpellType : byte {
        RUNE,
        MONSTER,
        PLAYER_SAY
    };

    public enum FindDistance {
        DISTANCE_BESIDE,
        DISTANCE_CLOSE,
        DISTANCE_FAR,
        DISTANCE_VERYFAR,
    };

    public enum FindLevel {
        LEVEL_HIGHER,
        LEVEL_LOWER,
        LEVEL_SAME,
    }

    public enum FindDirection {
        DIR_N,
        DIR_S,
        DIR_E,
        DIR_W,
        DIR_NE,
        DIR_NW,
        DIR_SE,
        DIR_SW,
    };

    public enum Fluids : byte {
        FLUID_NONE = 0,
        FLUID_WATER = 1,
        FLUID_BLOOD = 2,
        FLUID_BEER = 3,
        FLUID_SLIME = 4,
        FLUID_LEMONADE = 5,
        FLUID_MILK = 6,
        FLUID_MANAFLUID = 7,
        FLUID_WINE = 7
    };
}
