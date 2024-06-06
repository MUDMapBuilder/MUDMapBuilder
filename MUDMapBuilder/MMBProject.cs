using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace MUDMapBuilder
{
	public class MMBProject
	{
		private class RoomConnectionConverter : JsonConverter<MMBRoomConnection>
		{
			public override MMBRoomConnection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var vnum = reader.GetInt32();
				return new MMBRoomConnection
				{
					RoomId = vnum
				};
			}

			public override void Write(Utf8JsonWriter writer, MMBRoomConnection value, JsonSerializerOptions options)
			{
				writer.WriteNumberValue(value.RoomId);
			}
		}

		public MMBArea Area { get; set; }
		public BuildOptions BuildOptions { get; set; }

		public MMBProject()
		{
		}

		public MMBProject(MMBArea area, BuildOptions buildOptions)
		{
			Area = area;
			BuildOptions = buildOptions;
		}

		private static JsonSerializerOptions CreateJsonOptions()
		{
			var result = new JsonSerializerOptions
			{
				WriteIndented = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.Never,
				IncludeFields = true,
				IgnoreReadOnlyFields = true,
				IgnoreReadOnlyProperties = true,
			};

			result.Converters.Add(new JsonStringEnumConverter());
			result.Converters.Add(new RoomConnectionConverter());

			return result;
		}

		public string ToJson()
		{
			var options = CreateJsonOptions();
			return JsonSerializer.Serialize(this, options);
		}

		public static MMBProject Parse(string data)
		{
			var options = CreateJsonOptions();
			var project = JsonSerializer.Deserialize<MMBProject>(data, options);
			var area = project.Area;

			// Set directions
			foreach (var room in area.Rooms)
			{
				foreach (var pair in room.Connections)
				{
					pair.Value.Direction = pair.Key;
				}
			}

			return project;
		}

		public MMBProject Clone()
		{
			return new MMBProject(Area.Clone(), BuildOptions);
		}
	}
}