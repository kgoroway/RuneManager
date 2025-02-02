﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using RuneOptim.Management;

namespace RuneOptim.swar {
    // The monster stores its base stats in its base class
    public class Monster : Stats, IComparable<Monster> {
        [JsonProperty("name")]
        private string name;

        public string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }

        [JsonIgnore]
        public string FullName {
            get {
                if (IsHomunculus)
                    return HomunculusName;
                return (Awakened == 1 ? "" : Element.ToString() + " ") + (Name ?? "Missingno");
            }
            set {
                name = value;
            }
        }

        [JsonProperty("unit_id")]
        public ulong Id = 0;

        [JsonProperty("wizard_id")]
        public ulong WizardId = 0;

        [JsonProperty("class")]
        public int Grade;

        [JsonProperty("unit_level")]
        public int Level = 1;

        [JsonProperty("unit_master_id")]
        public int MonsterTypeId;

        public int GetFamily { get { return MonsterTypeId / 100; } }

        [JsonProperty("building_id")]
        public ulong BuildingId;

        [JsonProperty("create_time")]
        public DateTime? CreatedOn = null;

        private static Dictionary<int, MonsterStat> monDefs = null;

        private static Dictionary<int, MonsterStat> MonDefs {
            get {
                if (monDefs == null) {
                    monDefs = new Dictionary<int, MonsterStat>();
                    foreach (var item in MonsterStatList) {
                        if (!monDefs.ContainsKey(item.MonsterTypeId))
                            monDefs.Add(item.MonsterTypeId, item);
                    }
                }
                return monDefs;
            }
        }

        private static Dictionary<int, SkillDef> skillDefs = null;
        private static Dictionary<int, SkillDef> SkillDefs {
            get {
                if (skillDefs == null) {
                    skillDefs = new Dictionary<int, SkillDef>();
                    foreach (var item in MonsterStatList) {
                        foreach (var skill in item.Skills) {
                            if (!skillDefs.ContainsKey(skill.Com2usId))
                                skillDefs.Add(skill.Com2usId, skill);
                        }
                        foreach (var skill in item.HomunculusSkills) {
                            if (!skillDefs.ContainsKey(skill.Skill.Com2usId))
                                skillDefs.Add(skill.Skill.Com2usId, skill.Skill);
                        }
                    }
                }
                return skillDefs;
            }
        }

        [JsonProperty("attribute")]
        public Element Element;

        [JsonProperty("skills")]
        private IList<Skill> skilllist = null;

        [JsonIgnore]
        public IList<Skill> SkillList {
            get {
                if (skilllist == null)
                    skilllist = new List<Skill>();
                return skilllist;
            }
            set {
                skilllist = value;
            }
        }

        [JsonIgnore]
        public int SkillupsLevel { get { checkSkillups(); return SkillupLevel.Sum() - SkillupLevel.Count(i => i > 0); } }

        [JsonIgnore]
        public int SkillupsTotal { get { checkSkillups(); return SkillupMax.Sum() - SkillupMax.Count(i => i > 0); } }

        [JsonConverter(typeof(RuneLoadConverter))]
        [JsonProperty("runes")]
        private Rune[] runes;

        public Rune[] Runes {
            get {
                return runes ?? (runes = new Rune[0]);
            }
            set {
                runes = value;
            }
        }

        public int Priority = 0;

        public bool Locked = false;

        [JsonIgnore]
        public bool Downloaded = false;

        [JsonIgnore]
        public double Score = 0;

        [JsonIgnore]
        private Stats curStats = null;

        [JsonIgnore]
        private bool changeStats = true;

        [JsonIgnore]
        public bool InStorage = false;

        [JsonProperty("homunculus")]
        public bool IsHomunculus = false;

        [JsonProperty("homunculus_name")]
        public string HomunculusName;

        [JsonIgnore]
        public int LoadOrder = int.MaxValue;

        [JsonIgnore]
        public bool IsRep = false;

        [JsonIgnore]
        public bool OnDefense = false;

        [JsonIgnore]
        public override int ExtraCritRate {
            get {
                return base.ExtraCritRate;
            }
            set {
                changeStats = true;
                base.ExtraCritRate = value;
            }
        }

        public event EventHandler<RuneChangeEventArgs> OnRunesChanged;

        public int GameSpeedBonus { get {
                double speed = 0;
                if (Current != null) {
                    speed += Math.Ceiling(Speed * Current.SpeedPercent / 100f);
                    speed += Current.Runes?.Where(r => r != null).Sum(r => r.Speed[0]) ?? 0;
                }

                return (int)speed;
            }
        }

        public int SwapCost(Loadout l) {
            int cost = 0;
            for (int i = 0; i < 6; i++) {
                if (l.Runes[i] != null && l.Runes[i].AssignedName != FullName) {
                    // unequip current rune
                    if (Current.Runes[i] != null)
                        cost += Current.Runes[i].UnequipCost;
                    // unequip new rune from host
                    if (l.Runes[i].IsUnassigned && !l.Runes[i].Swapped) {
                        cost += l.Runes[i].UnequipCost;
                    }
                }
            }
            return cost;
        }

        // what is currently equiped for this instance of a monster
        private Loadout current = null;

        [JsonIgnore]
        public Loadout Current {
            get {
                if (current == null)
                    current = new Loadout() {
                        Element = Element,
                    };
                return current;
            }
            set {
                current = value;
            }
        }

        public Monster() {
        }

        // copy down!
        public Monster(Monster rhs, bool loadout = false) : base(rhs) {
            FullName = rhs.FullName;
            Id = rhs.Id;
            Level = rhs.Level;
            MonsterTypeId = rhs.MonsterTypeId;
            Grade = rhs.Grade;
            Element = rhs.Element;
            if (skilllist != null) {
                if (rhs.skilllist != null)
                    skilllist = skilllist.Concat(rhs.skilllist).ToList();
            }
            else if (rhs.skilllist != null) {
                // todo: do we *need* the copy?
                if (loadout)
                    skilllist = rhs.skilllist;
                else
                    skilllist = rhs.skilllist.ToList();
            }

            Priority = rhs.Priority;
            Downloaded = rhs.Downloaded;
            InStorage = rhs.InStorage;

            if (loadout) {
                Current = new Loadout(rhs.Current, rhs.Current.FakeLevel, rhs.Current.PredictSubs);
            }
            else {
                curStats = new Stats();
            }

        }

        // put this rune on the current build
        public Rune ApplyRune(Rune rune, int checkOn = 2) {
            var old = Current.AddRune(rune, checkOn);
            changeStats = true;
            if (!Current.TempLoad)
                OnRunesChanged?.Invoke(this, new RuneChangeEventArgs() { NewRune = rune, OldRune = old });
            return old;
        }

        public Rune RemoveRune(int slot) {
            var r = Current.RemoveRune(slot);
            changeStats = true;
            if (!Current.TempLoad)
                OnRunesChanged?.Invoke(this, new RuneChangeEventArgs() { OldRune = r });
            return r;
        }

        public void RefreshStats() {
            changeStats = true;
            GetStats();
            if (!Current.TempLoad)
                OnRunesChanged?.Invoke(this, new RuneChangeEventArgs() { });
        }

        private static MonsterStat[] monsterStatList = null;

        private static MonsterStat[] MonsterStatList {
            get {
                if (monsterStatList == null)
                    monsterStatList = JsonConvert.DeserializeObject<MonsterStat[]>(File.ReadAllText(Properties.Resources.SkillsJSON, System.Text.Encoding.UTF8));

                return monsterStatList;
            }
        }

        public int Awakened {
            get {
                return MonsterTypeId / 10 - MonsterTypeId / 100 * 10;
            }
        }

        // get the stats of the current build.
        // NOTE: the monster will contain it's base stats
        public Stats GetStats(bool force = false) {
            if (changeStats || Current.Changed || force) {
                checkSkillups();
                Current.Element = Element;
                Current.GetStats(this, ref curStats);
                curStats.SkillupDamage = SkillupDamage;
                curStats.DamageMultiplier = DamageMultiplier;
                changeStats = false;
            }

            return curStats;
        }

        private void checkSkillups() {
            if (skilllist != null && DamageMultiplier == null && MonDefs.ContainsKey(MonsterTypeId)) {
                MonsterDefinitions.MultiplierGroup average = new MonsterDefinitions.MultiplierGroup();

                int skdmg = 0;
                int i = -1;

                foreach (var si in skilllist) {
                    i++;
                    SkillupLevel[i] = skilllist[i].Level ?? 0;
                    if (SkillupMax[i] == 0)
                        SkillupMax[i] = SkillupLevel[i];
                    if (!SkillDefs.ContainsKey(si.SkillId ?? 0))
                        continue;
                    var ss = SkillDefs[si.SkillId ?? 0];
                    var df = JsonConvert.DeserializeObject<MonsterDefinitions.MultiplierGroup>(ss.MultiplierFormulaRaw, new MonsterDefinitions.MultiplierGroupConverter());
                    if (df.Props.Count > 0) {
                        var levels = ss.LevelProgressDescription.Split('\n').Take(skilllist[i].Level - 1 ?? 0);
                        SkillupMax[i] = ss.LevelProgressDescription.Split('\n').Length;
                        var ct = levels.Count(s => s == "Cooltime Turn -1");
                        int cooltime = (ss.Cooltime ?? 1) - ct;
                        SkillTimes[i] = cooltime;
                        var dmg = levels.Where(s => s.StartsWith("Damage")).Select(s => int.Parse(s.Replace("%", "").Replace("Damage +", "")));

                        DamageSkillups[i] = dmg.Any() ? dmg.Sum() : 0;
                        skdmg += (dmg.Any() ? dmg.Sum() : 0) / cooltime;

                        SkillsFunction[i] = Expression.Lambda<Func<Stats, double>>(df.AsExpression(StatType), StatType).Compile();

                        if (i != 0) {
                            df.Props.Last().Op = MonsterDefinitions.MultiplierOperator.Div;
                            df.Props.Add(new MonsterDefinitions.MultiplierValue(cooltime));
                            if (average.Props.Any())
                                average.Props.Last().Op = MonsterDefinitions.MultiplierOperator.Add;
                        }
                        average.Props.Add(new MonsterDefinitions.MultiplierValue(df));
                    }
                }
                DamageMultiplier = average;
                SkillupDamage = skdmg;
            }
        }

        // NYI comparison
        public EquipCompare CompareTo(Monster rhs) {
            if (Loadout.CompareSets(Current.Sets, rhs.Current.Sets) == 0)
                return EquipCompare.Unknown;

            Stats a = GetStats();
            Stats b = rhs.GetStats();

            if (a.Health <= b.Health)
                return EquipCompare.Worse;
            if (a.Attack <= b.Attack)
                return EquipCompare.Worse;
            if (a.Defense <= b.Defense)
                return EquipCompare.Worse;
            if (a.Speed <= b.Speed)
                return EquipCompare.Worse;
            if (a.CritRate <= b.CritRate)
                return EquipCompare.Worse;
            if (a.CritDamage <= b.CritDamage)
                return EquipCompare.Worse;
            if (a.Accuracy <= b.Accuracy)
                return EquipCompare.Worse;
            if (a.Resistance <= b.Resistance)
                return EquipCompare.Worse;

            return EquipCompare.Better;
        }

        public override string ToString() {
            return Id + " " + FullName + " lvl. " + Level;
        }

        int IComparable<Monster>.CompareTo(Monster other) {
            var comp = other.Grade - Grade;
            if (comp != 0) return comp;
            comp = other.Level - Level;
            if (comp != 0) return comp;
            comp = (int)Element - (int)other.Element;
            if (comp != 0) return comp;
            comp = other.Awakened - Awakened;
            if (comp != 0) return comp;
            comp = LoadOrder - other.LoadOrder;
            return comp;
        }
    }

    public class Skill : ListProp<int?> {
        // TODO: name
        [ListProperty(0)]
        public int? SkillId = null;
        [ListProperty(1)]
        public int? Level = null;

        protected override int maxInd {
            get {
                return 2;
            }
        }
    }



    public class RuneLoadConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(IList<RuneOptim.swar.Rune>).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer) {
            JToken tok = JToken.Load(reader);
            if (tok is JArray) {
                return tok.ToObject<RuneOptim.swar.Rune[]>();
            }
            else if (tok is JObject) {
                var jo = tok as JObject;
                return jo.Children().Select(o => o.First.ToObject<RuneOptim.swar.Rune>()).ToArray();
            }
            throw new InvalidCastException("A monsters runes are in an invalid format.");
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer) {
            if (value is Array) {
                // TODO: not having 6 runes is a mess
                var a = value as Array;
                //if (a.Length == 6)
                {
                    var ja = JArray.FromObject(value);
                    ja.WriteTo(writer);
                    return;
                }
            }
            throw new NotImplementedException();
        }
    }
}
