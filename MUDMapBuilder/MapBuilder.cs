using GoRogue;
using System;
using System.Collections.Generic;
using System.Drawing;
using static MUDMapBuilder.RoomsCollection;

namespace MUDMapBuilder
{
	public static class MapBuilder
	{
		private static readonly MMBDirection[] PushDirections = new MMBDirection[]
		{
			MMBDirection.North, MMBDirection.South, MMBDirection.West, MMBDirection.East
		};

		public static RoomsCollection Build(IMMBRoom[] sourceRooms, int? maxSteps = null, int? maxCompactRuns = null)
		{
			var toProcess = new List<MMBRoom>();
			var firstRoom = new MMBRoom(sourceRooms[0])
			{
				Position = new Point(0, 0)
			};
			toProcess.Add(firstRoom);

			var rooms = new RoomsCollection
			{
				firstRoom
			};

			// First run: we move through room's exits, assigning each room a 2d coordinate
			// If there are overlaps, then we expand the grid in the direction of movement
			int vc;
			var step = 1;
			Point pos;
			while (toProcess.Count > 0 && (maxSteps == null || maxSteps.Value > step))
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);

				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);
					if (rooms.GetRoomById(exitRoom.Id) != null)
					{
						continue;
					}

					pos = room.Position;
					var delta = exitDir.GetDelta();
					var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);


					vc = rooms.CalculateBrokenConnections();

					// Expand grid either if the new position is occupied by a room
					// Or if it breaks existing connection
					var expandGrid = false;
					var existingRoom = rooms.GetRoomByPosition(newPos);
					if (existingRoom != null)
					{
						expandGrid = true;
					}
					else
					{
						//
						var cloneRooms = rooms.Clone();
						var cloneRoom = new MMBRoom(exitRoom)
						{
							Position = newPos
						};

						cloneRooms.Add(cloneRoom);
						if (cloneRooms.CalculateBrokenConnections() > vc)
						{
							expandGrid = true;
						}
					}

					if (expandGrid)
					{
						// Push rooms in the movement direction
						rooms.ExpandGrid(newPos, delta);
					}

					var mbRoom = new MMBRoom(exitRoom)
					{
						Position = newPos
					};
					toProcess.Add(mbRoom);
					rooms.Add(mbRoom);
				}

				++step;
			}

			rooms.FixPlacementOfSingleExitRooms();

			// Next run: try to straighten the connections
/*			for (var it = 0; it < 10; ++it)
			{
				rooms.FixPlacementOfSingleExitRooms();

				vc = rooms.CalculateBrokenConnections();
				var roomsCount = rooms.Count;
				for (var i = 0; i < roomsCount; ++i)
				{
					var room = rooms[i];
					var brokenType = ConnectionBrokenType.NotBroken;
					var exitDirs = room.Room.ExitsDirections;
					var exitDir = MMBDirection.West;
					MMBRoom targetRoom = null;
					for (var j = 0; j < exitDirs.Length; ++j)
					{
						exitDir = exitDirs[j];

						var exitRoom = room.Room.GetRoomByExit(exitDir);
						targetRoom = rooms.GetRoomById(exitRoom.Id);
						if (targetRoom == null)
						{
							continue;
						}

						brokenType = rooms.CheckConnectionBroken(room, targetRoom, exitDir);
						if (brokenType != ConnectionBrokenType.NotBroken)
						{
							break;
						}
					}

					if (brokenType == ConnectionBrokenType.NotStraight)
					{
						var sourcePos = room.Position;
						var targetPos = targetRoom.Position;
						var desiredPos = new Point();
						switch (exitDir)
						{
							case MMBDirection.North:
								desiredPos = new Point(sourcePos.X, targetPos.Y < sourcePos.Y ? targetPos.Y : sourcePos.Y - 1);
								break;
							case MMBDirection.South:
								desiredPos = new Point(sourcePos.X, targetPos.Y > sourcePos.Y ? targetPos.Y : targetPos.Y + 1);
								break;
							case MMBDirection.East:
								desiredPos = new Point(targetPos.X > sourcePos.X ? targetPos.X : sourcePos.X + 1, sourcePos.Y);
								break;
							case MMBDirection.West:
								desiredPos = new Point(targetPos.X < sourcePos.X ? targetPos.X : sourcePos.X - 1, sourcePos.Y);
								break;
							case MMBDirection.Up:
								break;
							case MMBDirection.Down:
								break;
						}

						var d = new Point(desiredPos.X - targetPos.X, desiredPos.Y - targetPos.Y);

						var clone = rooms.Clone();
						var targetRoomClone = clone.GetRoomById(targetRoom.Id);

						clone.PushRoom(targetRoomClone, d);

						var sourceRoomClone = clone.GetRoomById(room.Id);
						brokenType = clone.CheckConnectionBroken(sourceRoomClone, targetRoomClone, exitDir);

						var c = clone.CalculateBrokenConnections();
						if (brokenType == ConnectionBrokenType.NotBroken && c <= vc)
						{
							// Connection was fixed
							rooms = clone;
							vc = c;
						}
					}
					else if (brokenType == ConnectionBrokenType.HasObstacles)
					{
						var deltas = new List<Point>();
						for(var x = -5; x <= 5; ++x)
						{
							for(var y = -5; y <= 5; ++y)
							{
								if (x == 0 && y == 0)
								{
									continue;
								}

								deltas.Add(new Point(x, y));
							}
						}

						foreach (var d in deltas)
						{
							var clone = rooms.Clone();
							var targetRoomClone = clone.GetRoomById(room.Id);

							clone.PushRoom(targetRoomClone, d);

							var sourceRoomClone = clone.GetRoomById(room.Id);
							brokenType = clone.CheckConnectionBroken(sourceRoomClone, targetRoomClone, exitDir);

							var c = clone.CalculateBrokenConnections();
							if (brokenType == ConnectionBrokenType.NotBroken && c <= vc)
							{
								// Connection was fixed
								rooms = clone;
								vc = c;
							}
						}
					}
				}
			}*/

			// Third run: Try to make the map more compact
			/*			var compactRuns = maxCompactRuns ?? 10;
						for (var it = 0; it < compactRuns; ++it)
						{
							foreach (var room in rooms)
							{
								pos = room.Position;
								var exitDirs = room.Room.ExitsDirections;
								for (var j = 0; j < exitDirs.Length; ++j)
								{
									var exitDir = exitDirs[j];

									// Works only with East and South directions
									if (exitDir != MMBDirection.East && exitDir != MMBDirection.South)
									{
										continue;
									}

									var exitRoom = room.Room.GetRoomByExit(exitDir);
									var targetRoom = rooms.GetRoomById(exitRoom.Id);
									if (targetRoom == null)
									{
										continue;
									}

									var delta = exitDir.GetDelta();
									var desiredPos = new Point(pos.X + delta.X, pos.Y + delta.Y);
									if (targetRoom.Position == desiredPos)
									{
										// The target room is already at desired pos
										continue;
									}

									// Skip if direction is Up/Down or the target room isn't straighly positioned
									var steps = 0;
									switch (exitDir)
									{
										case MMBDirection.North:
										case MMBDirection.South:
											if (targetRoom.Position.X - pos.X != 0)
											{
												continue;
											}

											steps = Math.Abs(targetRoom.Position.Y - pos.Y) - 1;
											break;
										case MMBDirection.West:
										case MMBDirection.East:
											if (targetRoom.Position.Y - pos.Y != 0)
											{
												continue;
											}

											steps = Math.Abs(targetRoom.Position.X - pos.X) - 1;
											break;
										case MMBDirection.Up:
										case MMBDirection.Down:
											// Skip Up/Down for now
											continue;
									}

									// Determine best amount of steps
									vc = rooms.CalculateBrokenConnections();
									int? bestSteps = null;
									while (steps > 0)
									{
										var cloneRooms = rooms.Clone();
										var sourceRoomClone = cloneRooms.GetRoomById(room.Id);
										var targetRoomClone = cloneRooms.GetRoomById(targetRoom.Id);

										cloneRooms.ApplyForceToRoom(sourceRoomClone, targetRoomClone, exitDir.GetOppositeDirection(), steps);

										var vc2 = cloneRooms.CalculateBrokenConnections();
										if ((vc2 <= vc && bestSteps == null) || vc2 < vc)
										{
											bestSteps = steps;
											vc2 = vc;
										}

										--steps;
									}

									if (bestSteps != null)
									{
										delta = exitDir.GetOppositeDirection().GetDelta();
										delta.X *= bestSteps.Value;
										delta.Y *= bestSteps.Value;

										rooms.ApplyForceToRoom(room, targetRoom, exitDir.GetOppositeDirection(), bestSteps.Value);
										goto nextiter;
									}
								}
							}

						nextiter:;
						}*/

			rooms.Steps = step;

			return rooms;
		}
	}
}