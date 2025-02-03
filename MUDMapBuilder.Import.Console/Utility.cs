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

		public static MMBRoom ToMMBRoom(this Room room, Area area)
		{
			var result = new MMBRoom(room.VNum, $"{room.Name} #{room.VNum}");

			foreach (var pair in room.Exits)
			{
				var exit = pair.Value;
				if (exit == null || exit.TargetRoom == null)
				{
					continue;
				}

				var conn = new MMBRoomConnection(pair.Key.ToMMBDirection(), exit.TargetRoom.VNum);

				foreach(var reset in area.Resets)
				{
					if (reset.ResetType != AreaResetType.Door || reset.Value2 != room.VNum || reset.Value3 != (int)pair.Key || reset.Value4 != 2)
					{
						continue;
					}

					conn.IsDoor = true;

					// Add locked door
					if (conn.DoorSigns == null)
					{
						conn.DoorSigns = new List<MMBRoomContentRecord>();
					}

					if (!exit.Flags.Contains(RoomExitFlags.PickProof))
					{
						conn.DoorColor = Color.CornflowerBlue;
					} else
					{
						conn.DoorColor = Color.IndianRed;
					}

					if (exit.KeyObject != null)
					{
						conn.DoorSigns.Add(new MMBRoomContentRecord($"{exit.KeyObject.ShortDescription} #{exit.KeyObject.VNum}", conn.DoorColor));
					}
				}

				result.Connections[pair.Key.ToMMBDirection()] = conn;

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
				result.Add(room.ToMMBRoom(area));
			}

			return result;
		}
	}
}
