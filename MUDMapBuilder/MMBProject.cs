using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBProject
	{
		private static JsonSerializerOptions _jsonOptions1 = CreateJsonOptions1();
		private static JsonSerializerOptions _jsonOptions2 = CreateJsonOptions2();

		public static JsonSerializerOptions JsonOptions1 => _jsonOptions1;
		public static JsonSerializerOptions JsonOptions2 => _jsonOptions2;

		public class ColorJsonConverter : JsonConverter<Color>
		{
			public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var str = reader.GetString();
				var result = ColorStorage.FromName(str);
				if (result == null )
				{
					throw new Exception($"Can't parse color '{str}'");
				}

				return result.Value;
			}

			public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
			{
				var str = string.Empty;

				var name = ColorStorage.GetColorName(value);
				if (!string.IsNullOrEmpty(name))
				{
					str = name.ToLower();
				} else
				{
					str = ColorStorage.ToHexString(value);
				}

				writer.WriteStringValue(str);
			}
		}

		private class RoomConnectionConverter : JsonConverter<MMBRoomConnection>
		{
			public override MMBRoomConnection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType == JsonTokenType.Number)
				{
					var vnum = reader.GetInt32();
					return new MMBRoomConnection
					{
						RoomId = vnum
					};
				}

				var doc = JsonDocument.ParseValue(ref reader);
				return JsonSerializer.Deserialize<MMBRoomConnection>(doc, JsonOptions1);
			}

			public override void Write(Utf8JsonWriter writer, MMBRoomConnection value, JsonSerializerOptions options)
			{
				if (!value.IsDoor)
				{
					writer.WriteNumberValue(value.RoomId);
				} else
				{
					JsonSerializer.Serialize(writer, value, JsonOptions1);
				}
			}
		}

		public MMBArea Area { get; set; }
		public BuildOptions BuildOptions { get; set; }

		public MMBProject()
		{
		}

		public MMBProject(MMBArea area, BuildOptions buildOptions = null)
		{
			Area = area;
			BuildOptions = buildOptions;
		}

		private static JsonSerializerOptions CreateJsonOptions1()
		{
			var result = new JsonSerializerOptions
			{
				WriteIndented = true,
				IncludeFields = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
				IgnoreReadOnlyFields = true,
				IgnoreReadOnlyProperties = true,
			};

			result.Converters.Add(new JsonStringEnumConverter());
			result.Converters.Add(new ColorJsonConverter());

			return result;
		}

		private static JsonSerializerOptions CreateJsonOptions2()
		{
			var result = CreateJsonOptions1();

			result.Converters.Add(new RoomConnectionConverter());

			return result;
		}

		public string ToJson()
		{
			var options = JsonOptions2;
			return JsonSerializer.Serialize(this, options);
		}

		public static MMBProject Parse(string data)
		{
			var options = JsonOptions2;
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