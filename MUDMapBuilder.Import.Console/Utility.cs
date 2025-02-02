using DikuLoad.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	internal static class Utility
	{
		public static string BuildStringValue(this GameObject obj)
		{
			switch (obj.ItemType)
			{
				case ItemType.Weapon:
					var r = $"{obj.Value2}d{obj.Value3}";
					if (!string.IsNullOrEmpty(obj.Value5) && obj.Value5 != "0")
					{
						if (obj.Value5.StartsWith("-"))
						{
							r += obj.Value5;
						}
						else
						{
							r += "+" + obj.Value5;
						}
					}

					return r;

				case ItemType.Armor:
					return obj.Value1.ToString();
			}

			return string.Empty;
		}

		public static string BuildEffectsValue(this GameObject obj)
		{
			return string.Join("<br>", (from ef in obj.Effects select ef.ToString()).ToArray());
		}

		public static string BuildFlagsValue<T>(this T value) where T : Enum
		{
			var lines = new List<string>();
			foreach (T flg in Enum.GetValues(typeof(T)))
			{
				if ((int)(object)flg == 0 || !value.HasFlag(flg))
				{
					continue;
				}

				lines.Add(flg.ToString());
			}

			return string.Join("<br>", lines);
		}

		public static MMBDirection ToMMBDirection(this Direction dir) => (MMBDirection)dir;

		public static MMBRoom ToMMBRoom(this Room room)
		{
			var result = new MMBRoom(room.VNum, $"{room.Name} #{room.VNum}");

			foreach (var exit in room.Exits)
			{
				if (exit.Value == null || exit.Value.TargetRoom == null)
				{
					continue;
				}

				result.Connections[exit.Key.ToMMBDirection()] = new MMBRoomConnection(exit.Key.ToMMBDirection(), exit.Value.TargetRoom.VNum);
			}

			return result;
		}

		public static MMBArea ToMMBArea(this Area area)
		{
			var result = new MMBArea
			{
				Name = area.Name,
				Credits = area.Builders,
				MinimumLevel = area.MinimumLevel,
				MaximumLevel = area.MaximumLevel
			};

			foreach (var room in area.Rooms)
			{
				result.Add(room.ToMMBRoom());
			}

			return result;
		}
	}
}
