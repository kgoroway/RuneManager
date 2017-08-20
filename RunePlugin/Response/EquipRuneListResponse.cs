﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RunePlugin;
using RuneOptim;
using Newtonsoft.Json.Linq;

namespace RunePlugin.Response {
	public class EquipRuneListResponse : SWResponse {
		[JsonProperty("unit_info")]
		public Monster TargetMonster;

		[JsonProperty("unequip_unit_list")]
		[JsonConverter(typeof(UnitListConverter))]
		public List<KeyValuePair<long, Monster>> SourceMonsters;

		[JsonProperty("equip_rune_id_list")]
		public long[] EquippedRuneIds;

		[JsonProperty("unequip_rune_id_list")]
		public long[] UnequippedRuneIds;

	}

	public class UnitListConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return typeof(IEnumerable<KeyValuePair<long, Monster>>).IsAssignableFrom(objectType);
		}

		public override bool CanRead {
			get {
				return base.CanRead;
			}
		}

		public override bool CanWrite {
			get {
				return base.CanWrite;
			}
		}

		public override object ReadJson(JsonReader reader,
			Type objectType, object existingValue, JsonSerializer serializer) {
			JToken tok = JToken.Load(reader);
			if (tok is JArray) {
				return tok.ToObject<List<KeyValuePair<long, Monster>>>();
			}
			else if (tok is JObject) {
				var jo = tok as JObject;
				return jo.Children().Select(o => new KeyValuePair<long, Monster>(long.Parse((o as JProperty).Name),  o.First.ToObject<Monster>())).ToList();
			}
			throw new InvalidCastException("A unit list is in an invalid format.");
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