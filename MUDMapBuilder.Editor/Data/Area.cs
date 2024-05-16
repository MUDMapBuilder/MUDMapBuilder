using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace MUDMapBuilder.Editor.Data
{
	public class Area
	{
		public string Name { get; set; }
		public List<Room> Rooms { get; } = new List<Room>();

		public static Area Parse(string data)
		{
			var rootObject = JsonNode.Parse(data);

			var result = new Area
			{
				Name = (string)rootObject["name"]
			};

			// First run: load all rooms
			var roomsObject = (JsonArray)rootObject["rooms"];
			var roomsDict = new Dictionary<int, Room>();
			foreach (var roomObject in roomsObject)
			{
				var room = new Room
				{
					Id = (int)roomObject["id"],
					Name = (string)roomObject["name"],
				};

				if (roomObject["otherAreaExit"] != null)
				{
					room.IsExitToOtherArea = (bool)roomObject["otherAreaExit"];
				}
					
				if (roomsDict.ContainsKey(room.Id))
				{
					throw new Exception($"Room with id {room.Id} already exists.");
				}

				roomsDict[room.Id] = room;
			}

			// Second run: exits
			foreach (var roomObject in roomsObject)
			{
				var room = roomsDict[(int)roomObject["id"]];
				var exitsObject = (JsonObject)roomObject["exits"];
				if (exitsObject != null)
				{
					foreach (var pair in exitsObject)
					{
						var dir = Enum.Parse<MMBDirection>(pair.Key);
						var roomId = (int)pair.Value;
						var targetRoom = roomsDict[roomId];

						room.InternalExits[dir] = targetRoom;
					}
				}
			}

			result.Rooms.AddRange(roomsDict.Values);

			return result;
		}
	}
}
