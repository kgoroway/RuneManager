﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RuneOptim.swar
{

    // Deserializes the .json into this
    public class Save {
        [JsonProperty("unit_list")]
        public readonly ObservableCollection<Monster> Monsters = new ObservableCollection<Monster>();

        [JsonProperty("deco_list")]
        public readonly ObservableCollection<Deco> Decorations = new ObservableCollection<Deco>();

        [JsonProperty("rune_craft_item_list")]
        public readonly ObservableCollection<Craft> Crafts = new ObservableCollection<Craft>();

        [JsonProperty("runes")]
        public readonly ObservableCollection<Rune> Runes = new ObservableCollection<Rune>();

        [JsonProperty("inventory_info")]
        public readonly ObservableCollection<InventoryItem> InventoryItems = new ObservableCollection<InventoryItem>();

        [JsonProperty("unit_lock_list")]
        public readonly ObservableCollection<ulong> LockedUnits = new ObservableCollection<ulong>();

        [JsonProperty("rune_lock_list")]
        public readonly ObservableCollection<ulong> LockedRunes = new ObservableCollection<ulong>();

        [JsonProperty("building_list")]
        public readonly ObservableCollection<Building> Buildings = new ObservableCollection<Building>();

        [JsonProperty("defense_unit_list")]
        public readonly ObservableCollection<DefensePlacement> DefenseUnits = new ObservableCollection<DefensePlacement>();

        [JsonProperty("guildwar_defense_unit_list")]
        public readonly ObservableCollection<DefensePlacement[]> GuildDefenseUnits = new ObservableCollection<DefensePlacement[]>();

        [JsonProperty("wizard_info")]
        public WizardInfo WizardInfo;

        [JsonProperty("guild")]
        public Guild Guild;

        // builds from rune optimizer don't match mine.
        // Don't care right now, perhaps a fuzzy-import later?
        [JsonProperty("savedBuilds")]
        public IList<object> Builds;

        [JsonIgnore]
        public readonly Stats Shrines = new Stats();

        [JsonProperty("modified")]
        public bool IsModified = false;

        [JsonIgnore]
        static Dictionary<int, string> monIdNames = null;

        public static Dictionary<int, string> MonIdNames {
            get {
                if (monIdNames == null)
                    monIdNames = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(Properties.Resources.MonstersJSON, System.Text.Encoding.UTF8));
                return monIdNames;
            }
        }

        [JsonIgnore]
        public int Priority = 1;

        [JsonIgnore]
        private int monLoaded = 0;

        public Save() {
            Runes.CollectionChanged += Runes_CollectionChanged;
            Monsters.CollectionChanged += Monsters_CollectionChanged;
            Decorations.CollectionChanged += Decorations_CollectionChanged;
            LockedUnits.CollectionChanged += LockedUnits_CollectionChanged;
            LockedRunes.CollectionChanged += LockedRunes_CollectionChanged;
            Buildings.CollectionChanged += Buildings_CollectionChanged;
            DefenseUnits.CollectionChanged += DefenseUnits_CollectionChanged;
            GuildDefenseUnits.CollectionChanged += GuildDefenseUnits_CollectionChanged;
        }

        public Save(Save rhs) : this()
        {
            Runes.AddRange(rhs.Runes);
            Monsters.AddRange(rhs.Monsters);
            Crafts.AddRange(rhs.Crafts);
            InventoryItems.AddRange(rhs.InventoryItems);
            Decorations.AddRange(rhs.Decorations);
            LockedUnits.AddRange(rhs.LockedUnits);
            LockedRunes.AddRange(rhs.LockedRunes);
            Buildings.AddRange(rhs.Buildings);
            DefenseUnits.AddRange(rhs.DefenseUnits);
            GuildDefenseUnits.AddRange(rhs.GuildDefenseUnits);
            Priority = rhs.Priority;
            IsModified = rhs.IsModified;
            Shrines.CopyFrom(rhs.Shrines, true);
            Guild.CopyFrom(rhs.Guild, true);
            WizardInfo = rhs.WizardInfo;
        }

        public static int GetPiecesRequired(int monsterTypeId) {
            var a = monsterTypeId / 100;
            var b = MonIdNames[a];
            var c = MonsterStat.BaseStars(b);
            var d = InventoryItem.PiecesRequired(c);
            return d;
        }

        private void GuildDefenseUnits_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var d in e.NewItems.Cast<DefensePlacement[]>()) {
                        foreach (var dd in d) {
                            var m = GetMonster(dd.UnitId);
                            if (m != null)
                                m.OnDefense = true;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void DefenseUnits_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var d in e.NewItems.Cast<DefensePlacement>()) {
                        var m = GetMonster(d.UnitId);
                        if (m != null)
                            m.OnDefense = true;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void Buildings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var b in e.NewItems.Cast<Building>()) {
                        if (b.BuildingType == BuildingType.MonsterStorage) {
                            foreach (var m in Monsters.Where(mo => mo.BuildingId == b.Id)) {
                                m.InStorage = true;
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void LockedUnits_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems.Cast<ulong>()) {
                        var mon = GetMonster(i);
                        if (mon != null)
                            mon.Locked = true;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var i in e.OldItems.Cast<ulong>()) {
                        var mon = GetMonster(i);
                        if (mon != null)
                            mon.Locked = false;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void LockedRunes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems.Cast<ulong>()) {
                        var rune = GetRune(i);
                        if (rune != null)
                            rune.Locked = true;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var i in e.OldItems.Cast<ulong>()) {
                        var rune = GetRune(i);
                        if (rune != null)
                            rune.Locked = false;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void Decorations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var shr in e.NewItems.Cast<Deco>().Where(d => d.Shrine != Shrine.Unknown)) {
                        var i = Deco.ShrineStats.Keys.ToList().IndexOf(shr.Shrine.ToString());
                        double v = Deco.ShrineStats[shr.Shrine.ToString()][shr.Level];
                        Shrines[shr.Shrine.ToString()] = v;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }
        }

        private void Monsters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (Monster mon in e.NewItems) {
                        if (mon.Name == null)
                            mon.Name = MonIdNames.FirstOrDefault(m => m.Key == mon.MonsterTypeId).Value;
                        mon.LoadOrder = monLoaded++;
                        if (mon.Name == null) {
                            mon.Name = MonIdNames.FirstOrDefault(m => m.Key == mon.MonsterTypeId / 100).Value;
                        }
                        if (mon.Name == null) {
                            mon.Name = "MissingNo";
                        }
                        // Add the runes contained in the Monsters JSON definition to the Rune pool
                        foreach (var r in mon.Runes) {
                            if (!Runes.Any(ru => ru.Id == r.Id))
                                Runes.Add(r);
                        }
                        mon.Current.Shrines = Shrines;
                        mon.Current.Guild = Guild;

                        if (mon.BuildingId == Buildings.FirstOrDefault(b => b.BuildingType == BuildingType.MonsterStorage)?.Id)
                            mon.InStorage = true;

                        // Add all the Runes in the pool assigned to the monster to it's current loadout
                        foreach (Rune rune in Runes.Where(r => r.AssignedId == mon.Id)) {
                            mon.ApplyRune(rune);
                            rune.AssignedName = mon.FullName;
                            rune.Assigned = mon;
                        }

                        if (LockedUnits.Contains(mon.Id))
                            mon.Locked = true;

                        if (WizardInfo != null && WizardInfo.ReputationMonsterId == mon.Id)
                            mon.IsRep = true;

                        if (DefenseUnits.Any(d => d.UnitId == mon.Id) || GuildDefenseUnits.Any(d => d.Any(dd => dd.UnitId == mon.Id)))
                            mon.OnDefense = true;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var m in e.OldItems.Cast<Monster>()) {
                        foreach (var r in m.Current.Runes) {
                            if (r != null) {
                                r.Assigned = null;
                                r.AssignedId = 0;
                                r.AssignedName = "Inventory";
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    // todo: looks like collection is dead at this point :(
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                default:
                    throw new NotImplementedException("" + e.Action);
            }
        }

        private void Runes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    foreach (var r in e.NewItems.Cast<Rune>()) {
                        var rems = Runes.Where(ru => ru.Id == r.Id).ToList();
                        foreach (var ru in rems) {
                            if (ru != r) {
                                Runes.Remove(ru);
                                var mon = ru.Assigned ?? r.Assigned;
                                if (mon != null && mon.Current.Runes[r.Slot - 1] != r) {
                                    mon.ApplyRune(r);
                                    r.Assigned = mon;
                                }
                            }
                        }
                        r.PrebuildAttributes();
                        if (LockedRunes.Contains(r.Id))
                            r.Locked = true;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var r in e.OldItems.Cast<Rune>()) {
                        if (r.Assigned != null) {
                            r.Assigned.RemoveRune(r.Slot);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new NotImplementedException();
            }

        }

        // Ask for monsters nicely
        public Monster GetMonster(string name) {
            return getMonster(name, 1);
        }

        public Monster GetMonster(string name, int num) {
            return getMonster(name, num);
        }

        private Monster getMonster(string name, int num) {
            if (!System.Diagnostics.Debugger.IsAttached)
                RuneLog.Debug("GetMonster " + num + "th " + name + " from " + Monsters.Count);
            Monster mon = Monsters.Where(m => m.FullName == name).Skip(num - 1).FirstOrDefault();
            if (mon == null)
                mon = Monsters.FirstOrDefault(m => m.FullName == name);
            if (mon != null)
                return mon;
            return null;
        }

        public Monster GetMonster(ulong id) {
            if (!System.Diagnostics.Debugger.IsAttached)
                RuneLog.Debug("GetMonster " + id + " from " + Monsters.Count);
            return Monsters.FirstOrDefault(m => m.Id == id);
        }

        public Rune GetRune(ulong id) {
            if (!System.Diagnostics.Debugger.IsAttached)
                RuneLog.Debug("GetRune " + id + " from " + Runes.Count);
            return Runes.FirstOrDefault(r => r.Id == id);
        }
    }
}
