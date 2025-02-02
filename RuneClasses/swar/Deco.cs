﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RuneOptim.swar {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Shrine {
        [EnumMember(Value = "Unknown")]
        Unknown = -1,
        [EnumMember(Value = "")]
        Null = 0,

        [EnumMember(Value = "DEF")]
        DEF = 4,
        [EnumMember(Value = "SPD")]
        SPD = 6,
        [EnumMember(Value = "HP")]
        HP = 8,
        [EnumMember(Value = "ATK")]
        ATK = 9,

        [EnumMember(Value = "FireATK")]
        FireATK = 15,
        [EnumMember(Value = "WaterATK")]
        WaterATK = 16,
        [EnumMember(Value = "WindATK")]
        WindATK = 17,
        [EnumMember(Value = "LightATK")]
        LightATK = 18,
        [EnumMember(Value = "DarkATK")]
        DarkATK = 19,

        [EnumMember(Value = "CD")]
        CD = 31,

    }

    public class Deco {
        public static readonly string SPD = AttrStr.SPD;
        public static readonly string DEF = AttrStr.DEF;
        public static readonly string ATK = AttrStr.ATK;
        public static readonly string HP = AttrStr.HP;
        public static readonly string CD = AttrStr.CD;
        public static readonly string WATER_ATK = "Water" + ATK;
        public static readonly string FIRE_ATK = "Fire" + ATK;
        public static readonly string WIND_ATK = "Wind" + ATK;
        public static readonly string LIGHT_ATK = "Light" + ATK;
        public static readonly string DARK_ATK = "Dark" + ATK;

        public readonly static Dictionary<string, List<double>> ShrineStats = new Dictionary<string, List<double>>() {
            { SPD, new List<double> { 0, 1, 2, 3, 4, 4.5, 5, 5.5, 6, 6.5, 7, 8, 9, 10, 11, 11.5, 13, 13.5, 14, 14.5, 15 } },
            { DEF, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 } },
            { ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 } },
            { HP, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 } },
            { WATER_ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 } },
            { FIRE_ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 } },
            { WIND_ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 } },
            { LIGHT_ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 } },
            { DARK_ATK, new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 } },
            { CD, new List<double> { 0, 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13, 15, 16, 18, 19, 20, 22, 23, 24, 25} },
        };

        [JsonProperty("pos_x")]
        public int X = 0;

        [JsonProperty("pos_y")]
        public int Y = 0;

        [JsonProperty("deco_id")]
        public int ID;

        [JsonProperty("level")]
        public int Level;

        [JsonProperty("wizard_id")]
        public int Owner;

        [JsonProperty("island_id")]
        public int Island;

        [JsonProperty("master_id")]
        public int MasterId;

        [JsonIgnore]
        public Shrine Shrine {
            get {
                switch (MasterId) {
                    case 4:
                    case 6:
                    case 8:
                    case 9:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 31:
                        return (Shrine)MasterId;
                    default:
                        return Shrine.Unknown;
                }
            }
        }

        public override string ToString() {
            return "Lvl. " + Level + " " + Shrine;
        }
    }
}
