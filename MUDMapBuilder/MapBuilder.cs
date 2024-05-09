using System;
using System.Collections.Generic;
using System.Drawing;
using static MUDMapBuilder.RoomsCollection;

namespace MUDMapBuilder
{
	public static class MapBuilder
	{
		private static RoomsCollection FixRoom(RoomsCollection rooms, MMBRoom room, MMBRoom targetRoom, MMBDirection exitDir)
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
					desiredPos = new Point(sourcePos.X, targetPos.Y > sourcePos.Y ? targetPos.Y : sourcePos.Y + 1);
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

			return clone;
		}

		public static RoomsCollection Build(IMMBRoom[] sourceRooms, BuildOptions options = null)
		{
			if (options == null)
			{
				options = new BuildOptions();
			}

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
			var step = 1;
			Point pos;
			while (toProcess.Count > 0 && (options.Steps == null || options.Steps.Value > step))
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);

				foreach (var pair in room.Room.Exits)
				{
					var exitDir = pair.Key;
					var exitRoom = pair.Value;
					if (rooms.GetRoomById(exitRoom.Id) != null)
					{
						continue;
					}

					pos = room.Position;
					var delta = exitDir.GetDelta();
					var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);


					var vc = rooms.CalculateBrokenConnections().Count;

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
						if (cloneRooms.CalculateBrokenConnections().Count > vc)
						{
							expandGrid = true;
						}
					}

					if (expandGrid)
					{
						// Push rooms in the movement direction
						rooms.ExpandGrid(newPos, delta);
					}

					var newRoom = new MMBRoom(exitRoom)
					{
						Position = newPos
					};
					toProcess.Add(newRoom);
					rooms.Add(newRoom);
				}

				++step;
			}

			if (options.Straighten)
			{
				// Straighten run: try to make room connections more straight
				var runsLeft = 10;
				while (runsLeft > 0)
				{
					var vc = rooms.CalculateBrokenConnections();

					if (vc.Count == 0)
					{
						break;
					}

					var roomsCount = rooms.Count;
					for (var i = 0; i < roomsCount; ++i)
					{
						var room = rooms[i];
						var brokenType = ConnectionBrokenType.NotBroken;
						var exitDir = MMBDirection.West;
						MMBRoom targetRoom = null;
						foreach (var pair in room.Room.Exits)
						{
							exitDir = pair.Key;
							var exitRoom = pair.Value;
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
							var clone1 = FixRoom(rooms, room, targetRoom, exitDir);

							var sourceRoomClone = clone1.GetRoomById(room.Id);
							var targetRoomClone = clone1.GetRoomById(targetRoom.Id);
							brokenType = clone1.CheckConnectionBroken(sourceRoomClone, targetRoomClone, exitDir);

							var c1 = clone1.CalculateBrokenConnections();
							if (brokenType == ConnectionBrokenType.NotBroken &&
								c1.ConnectionsWithObstaclesCount <= vc.ConnectionsWithObstaclesCount &&
								c1.NonStraightConnectionsCount <= vc.NonStraightConnectionsCount)
							{
								// Connection was fixed
								rooms = clone1;
								goto finish;
							}

							// Now try the other way around
							var clone2 = FixRoom(rooms, targetRoom, room, exitDir.GetOppositeDirection());

							sourceRoomClone = clone2.GetRoomById(targetRoomClone.Id);
							targetRoomClone = clone2.GetRoomById(room.Id);

							brokenType = clone2.CheckConnectionBroken(sourceRoomClone, targetRoomClone, exitDir.GetOppositeDirection());

							var c2 = clone2.CalculateBrokenConnections();
							if (brokenType == ConnectionBrokenType.NotBroken &&
								c2.ConnectionsWithObstaclesCount <= vc.ConnectionsWithObstaclesCount &&
								c2.NonStraightConnectionsCount <= vc.NonStraightConnectionsCount)
							{
								// Connection was fixed
								rooms = clone2;
								goto finish;
							}

							// If neither ways are obviously good, then we select one that is better
							if (c1.ConnectionsWithObstaclesCount < c2.ConnectionsWithObstaclesCount ||
								(c1.ConnectionsWithObstaclesCount == c2.ConnectionsWithObstaclesCount &&
								c1.NonStraightConnectionsCount < c2.NonStraightConnectionsCount))
							{
								rooms = clone1;
								goto finish;
							}

							rooms = clone2;
							goto finish;
						}
						else if (brokenType == ConnectionBrokenType.HasObstacles)
						{
							var deltas = new List<Point>();
							for (var x = -5; x <= 5; ++x)
							{
								for (var y = -5; y <= 5; ++y)
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
								var targetRoomClone = clone.GetRoomById(targetRoom.Id);

								clone.PushRoom(targetRoomClone, d);

								var sourceRoomClone = clone.GetRoomById(room.Id);
								brokenType = clone.CheckConnectionBroken(sourceRoomClone, targetRoomClone, exitDir);

								var c = clone.CalculateBrokenConnections();
								if (brokenType == ConnectionBrokenType.NotBroken &&
									c.ConnectionsWithObstaclesCount < vc.ConnectionsWithObstaclesCount)
								{
									rooms = clone;
									goto finish;
								}
							}
						}
					}
				finish:;

					var vc2 = rooms.CalculateBrokenConnections();

					// If amount of broken connections hasn't changed, decrease amount of runs
					if (vc2.Count >= vc.Count)
					{
						--runsLeft;
					}
				}
			}

			if (options.Compact)
			{
				// Compact run: Try to make the map more compact
				for (var it = 0; it < 10; ++it)
				{
					for (var i = 0; i < rooms.Count; ++i)
					{
						var room = rooms[i];
						pos = room.Position;
						foreach (var pair in room.Room.Exits)
						{
							var exitDir = pair.Key;
							var exitRoom = pair.Value;

							if (exitDir != MMBDirection.East && exitDir != MMBDirection.South && exitDir != MMBDirection.Up)
							{
								continue;
							}

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
							var deltas = new List<Point>();
							switch (exitDir)
							{
								case MMBDirection.North:
								case MMBDirection.South:
									{
										if (targetRoom.Position.X - pos.X != 0)
										{
											continue;
										}

										var steps = Math.Abs(targetRoom.Position.Y - pos.Y) - 1;
										for(var j = steps; j >= 1; --j)
										{
											delta = exitDir.GetOppositeDirection().GetDelta();
											delta.X *= j;
											delta.Y *= j;
											deltas.Add(delta);
										}
									}
									break;
								case MMBDirection.West:
								case MMBDirection.East:
									{
										if (targetRoom.Position.Y - pos.Y != 0)
										{
											continue;
										}

										var steps = Math.Abs(targetRoom.Position.X - pos.X) - 1;
										for (var j = steps; j >= 1; --j)
										{
											delta = exitDir.GetOppositeDirection().GetDelta();
											delta.X *= j;
											delta.Y *= j;
											deltas.Add(delta);
										}
									}
									break;
								case MMBDirection.Up:
								case MMBDirection.Down:
									delta = new Point(desiredPos.X - targetRoom.Position.X, desiredPos.Y - targetRoom.Position.Y);
									deltas.Add(delta);
									break;
							}

							var vc = rooms.CalculateBrokenConnections();
							for(var j = 0; j < deltas.Count; ++j)
							{
								var cloneRooms = rooms.Clone();
								var targetRoomClone = cloneRooms.GetRoomById(targetRoom.Id);

								delta = deltas[j];
								cloneRooms.PushRoom(targetRoomClone, delta);

								var vc2 = cloneRooms.CalculateBrokenConnections();
								if (vc2.NonStraightConnectionsCount <= vc.NonStraightConnectionsCount &&
									vc2.ConnectionsWithObstaclesCount <= vc.ConnectionsWithObstaclesCount)
								{
									rooms = cloneRooms;
									break;
								}
							}
						}
					}
				}
			}

//			rooms.FixPlacementOfSingleExitRooms();

			rooms.Steps = step;

			return rooms;
		}
	}
}