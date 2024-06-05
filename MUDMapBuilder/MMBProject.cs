using System.Text.Json.Nodes;
using System.Text.Json;
using System;

namespace MUDMapBuilder
{
	public class MMBProject
	{
		public MMBArea Area { get; private set; }
		public BuildOptions BuildOptions { get; private set; }

		public MMBProject(MMBArea area, BuildOptions buildOptions)
		{
			Area = area;
			BuildOptions = buildOptions;
		}

		public string ToJson()
		{
			var areaObject = new JsonObject
			{
				["name"] = Area.Name
			};

			var optionsObject = new JsonObject
			{
				["maxSteps"] = BuildOptions.MaxSteps,
				["keepSolitaryRooms"] = BuildOptions.KeepSolitaryRooms,
				["fixObstacles"] = BuildOptions.FixObstacles,
				["fixNonStraight"] = BuildOptions.FixNonStraight,
				["fixIntersected"] = BuildOptions.FixIntersected,
				["compactMap"] = BuildOptions.CompactMap,
				["addDebugInfo"] = BuildOptions.AddDebugInfo,
				["colorizeConnectionIssues"] = BuildOptions.ColorizeConnectionIssues
			};

			areaObject["buildOptions"] = optionsObject;

			var roomsObject = new JsonArray();
			foreach (var room in Area)
			{
				var roomObject = new JsonObject
				{
					["id"] = room.Id,
					["name"] = room.Name,
				};

				if (room.IsExitToOtherArea)
				{
					roomObject["otherAreaExit"] = true;
				}

				var exitsObject = new JsonObject();
				foreach (var con in room.Connections.Values)
				{
					if (con.ConnectionType == MMBConnectionType.Backward)
					{
						continue;
					}

					exitsObject[con.Direction.ToString()] = con.RoomId;
				}

				roomObject["exits"] = exitsObject;
				roomsObject.Add(roomObject);
			}

			areaObject["rooms"] = roomsObject;

			var options = new JsonSerializerOptions { WriteIndented = true };
			return areaObject.ToJsonString(options);
		}

		public static MMBProject Parse(string data)
		{
			var rootObject = JsonNode.Parse(data);

			var area = new MMBArea
			{
				Name = (string)rootObject["name"]
			};

			var options = new BuildOptions();
			if (rootObject["buildOptions"] != null)
			{
				var optionsObject = (JsonObject)rootObject["buildOptions"];

				if (optionsObject["maxSteps"] != null)
				{
					options.MaxSteps = (int)optionsObject["maxSteps"];
				}

				if (optionsObject["keepSolitaryRooms"] != null)
				{
					options.KeepSolitaryRooms = (bool)optionsObject["keepSolitaryRooms"];
				}

				if (optionsObject["fixObstacles"] != null)
				{
					options.FixObstacles = (bool)optionsObject["fixObstacles"];
				}

				if (optionsObject["fixNonStraight"] != null)
				{
					options.FixNonStraight = (bool)optionsObject["fixNonStraight"];
				}

				if (optionsObject["fixIntersected"] != null)
				{
					options.FixIntersected = (bool)optionsObject["fixIntersected"];
				}

				if (optionsObject["compactMap"] != null)
				{
					options.CompactMap = (bool)optionsObject["compactMap"];
				}

				if (optionsObject["addDebugInfo"] != null)
				{
					options.AddDebugInfo = (bool)optionsObject["addDebugInfo"];
				}

				if (optionsObject["colorizeConnectionIssues"] != null)
				{
					options.ColorizeConnectionIssues = (bool)optionsObject["colorizeConnectionIssues"];
				}
			}

			// First run: load all rooms and exits
			var roomsObject = (JsonArray)rootObject["rooms"];
			foreach (var roomObject in roomsObject)
			{
				var isExitToOtherArea = false;
				if (roomObject["otherAreaExit"] != null)
				{
					isExitToOtherArea = (bool)roomObject["otherAreaExit"];
				}

				var room = new MMBRoom((int)roomObject["id"], (string)roomObject["name"], isExitToOtherArea);
				if (area.GetRoomById(room.Id) != null)
				{
					throw new Exception($"Room with id {room.Id} already exists.");
				}

				var exitsObject = (JsonObject)roomObject["exits"];
				if (exitsObject != null)
				{
					foreach (var pair in exitsObject)
					{
						var dir = Enum.Parse<MMBDirection>(pair.Key);
						var targetRoomId = (int)pair.Value;
						room.Connections[dir] = new MMBRoomConnection(dir, targetRoomId)
						{
							ConnectionType = MMBConnectionType.Forward
						};
					}
				}

				area.Add(room);
			}

			// Second run: set backward and two-way connections
			foreach (var room in area)
			{
				foreach (var connection in room.Connections.Values)
				{
					if (connection.RoomId == room.Id || connection.ConnectionType == MMBConnectionType.Backward)
					{
						continue;
					}

					var dir = connection.Direction;
					var oppDir = dir.GetOppositeDirection();

					var targetRoom = area.GetRoomById(connection.RoomId);
					if (targetRoom == null)
					{
						continue;
					}

					var foundOpposite = false;
					var oppositeConnection = targetRoom.FindConnection(room.Id);
					if (oppositeConnection != null &&
						oppDir == oppositeConnection.Direction)
					{
						foundOpposite = true;
					}

					if (foundOpposite)
					{
						connection.ConnectionType = MMBConnectionType.TwoWay;
					}
					else if (!targetRoom.Connections.ContainsKey(oppDir))
					{
						// Establish opposite backwards connection
						targetRoom.Connections[oppDir] = new MMBRoomConnection(oppDir, room.Id)
						{
							ConnectionType = MMBConnectionType.Backward
						};
					}
				}
			}

			return new MMBProject(area, options);
		}

		public MMBProject Clone()
		{
			return new MMBProject(Area.Clone(), BuildOptions);
		}
	}
}