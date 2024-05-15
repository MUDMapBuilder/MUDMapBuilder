using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	public class MapBuilder
	{
		private enum PositioningType
		{
			Straight,
			Obstacles,
			NonStraight,
			CompletelyIncorrect
		}

		private enum StraightenRoomResult
		{
			Success,
			Fail,
			OutOfSteps
		}

		private class StraightenConnectionResult
		{
			public Point Delta { get; private set; }
			public int RoomsRemoved { get; private set; }
			public BrokenConnectionsInfo BrokenConnections { get; private set; }

			public StraightenConnectionResult(Point delta, int roomsRemoved, BrokenConnectionsInfo brokenConnections)
			{
				Delta = delta;
				RoomsRemoved = roomsRemoved;
				BrokenConnections = brokenConnections;
			}
		}

		private class RemoveRoomRecord
		{
			public int RoomId { get; private set; }
			public Point Position { get; private set; }
			public int HashCode { get; private set; }

			public RemoveRoomRecord(int roomId, Point position, int hashCode)
			{
				RoomId = roomId;
				Position = position;
				HashCode = hashCode;
			}
		}

		private readonly IMMBRoom[] _sourceRooms;
		private readonly BuildOptions _options;
		private readonly List<MMBRoom> _toProcess = new List<MMBRoom>();
		private readonly HashSet<int> _removedRooms = new HashSet<int>();
		private readonly Dictionary<int, List<RemoveRoomRecord>> _removalHistory = new Dictionary<int, List<RemoveRoomRecord>>();
		private PositionedRooms _rooms;
		private readonly List<PositionedRooms> _history = new List<PositionedRooms>();

		private MapBuilder(IMMBRoom[] sourceRooms, BuildOptions options)
		{
			_sourceRooms = sourceRooms;
			_options = options ?? new BuildOptions();
		}

		private static Point CalculateDesiredPosition(Point sourcePos, Point targetPos, MMBDirection direction)
		{
			var desiredPos = new Point();
			switch (direction)
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
					desiredPos = new Point(targetPos.X > sourcePos.X ? targetPos.X : sourcePos.X + 1, targetPos.Y < sourcePos.Y ? targetPos.Y : sourcePos.Y - 1);
					break;
				case MMBDirection.Down:
					desiredPos = new Point(targetPos.X < sourcePos.X ? targetPos.X : sourcePos.X - 1, targetPos.Y > sourcePos.Y ? targetPos.Y : sourcePos.Y + 1);
					break;
				default:
					throw new Exception($"Unknown direction {direction}");
			}

			return desiredPos;
		}

		private bool AddRunStep()
		{
			_history.Add(_rooms.Clone());
			return _options.MaxSteps > _history.Count;
		}

		private static Dictionary<int, Point> MeasurePushRoom(PositionedRooms rooms, int firstRoomId, Point firstForceVector)
		{
			var roomsToPush = new Dictionary<int, Point>();

			var firstRoom = rooms.GetRoomById(firstRoomId);
			var toProcess = new List<Tuple<MMBRoom, Point>>
			{
				new Tuple<MMBRoom, Point>(firstRoom, firstForceVector)
			};

			while (toProcess.Count > 0)
			{
				var item = toProcess[0];
				var room = item.Item1;
				var pos = room.Position.Value;
				toProcess.RemoveAt(0);

				roomsToPush[room.Id] = item.Item2;

				// Process neighbour rooms
				foreach (var pair in room.Room.Exits)
				{
					var exitDir = pair.Key;
					var exitRoom = pair.Value;
					var forceVector = item.Item2;

					var targetRoom = rooms.GetRoomById(exitRoom.Id);
					if (targetRoom == null || targetRoom.Position == null || roomsToPush.ContainsKey(exitRoom.Id))
					{
						continue;
					}

					if (!PositionedRooms.IsConnectionStraight(room.Position.Value, targetRoom.Position.Value, exitDir))
					{
						// Skip broken connections
						continue;
					}

					var targetPos = targetRoom.Position.Value;
					switch (exitDir)
					{
						case MMBDirection.North:
							forceVector.Y += Math.Abs(targetPos.Y - pos.Y) - 1;
							if (forceVector.Y > 0)
							{
								forceVector.Y = 0;
							}
							break;

						case MMBDirection.East:
							forceVector.X -= Math.Abs(targetPos.X - pos.X) - 1;
							if (forceVector.X < 0)
							{
								forceVector.X = 0;
							}
							break;

						case MMBDirection.South:
							forceVector.Y -= Math.Abs(targetPos.Y - pos.Y) - 1;
							if (forceVector.Y < 0)
							{
								forceVector.Y = 0;
							}
							break;

						case MMBDirection.West:
							forceVector.X += Math.Abs(targetPos.X - pos.X) - 1;
							if (forceVector.X > 0)
							{
								forceVector.X = 0;
							}
							break;

						case MMBDirection.Up:
							forceVector.X -= Math.Abs(targetPos.X - pos.X) - 1;
							forceVector.Y += Math.Abs(targetPos.Y - pos.Y) - 1;

							if (forceVector.X < 0)
							{
								forceVector.X = 0;
							}

							if (forceVector.Y > 0)
							{
								forceVector.Y = 0;
							}
							break;

						case MMBDirection.Down:
							forceVector.X += Math.Abs(targetPos.X - pos.X) - 1;
							forceVector.Y -= Math.Abs(targetPos.Y - pos.Y) - 1;

							if (forceVector.X > 0)
							{
								forceVector.X = 0;
							}
							if (forceVector.Y < 0)
							{
								forceVector.Y = 0;
							}

							break;
					}

					if (forceVector.X != 0 || forceVector.Y != 0)
					{
						toProcess.Add(new Tuple<MMBRoom, Point>(targetRoom, forceVector));
					}
				}
			}

			return roomsToPush;
		}

		private bool PushRoom(PositionedRooms rooms, int firstRoomId, Point firstForceVector, bool measureRun, out int roomsRemoved)
		{
			var roomsToPush = MeasurePushRoom(rooms, firstRoomId, firstForceVector);

			var roomsToDelete = new List<MMBRoom>();
			var roomsToMove = new List<Tuple<MMBRoom, Point>>();
			foreach (var pair in roomsToPush)
			{
				var room = rooms.GetRoomById(pair.Key);
				var delta = pair.Value;

				var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);

				var existingRoom = rooms.GetRoomByPosition(newPos);
				if (existingRoom != null && !roomsToPush.ContainsKey(existingRoom.Id))
				{
					roomsToDelete.Add(existingRoom);
				}

				roomsToMove.Add(new Tuple<MMBRoom, Point>(room, delta));
			}

			roomsRemoved = roomsToDelete.Count;

			if (!measureRun)
			{
				if (roomsToDelete.Count > 0)
				{
					return RemoveRooms(roomsToDelete.ToArray());
				}

				// Mark for movement
				foreach (var tuple in roomsToMove)
				{
					var room = tuple.Item1;
					var delta = tuple.Item2;
					room.MarkColor = SKColors.YellowGreen;
					room.ForceMark = delta;
				}

				if (!AddRunStep())
				{
					return false;
				}

				// Do the movement
				foreach (var tuple in roomsToMove)
				{
					var room = tuple.Item1;
					room.MarkColor = null;
					room.ForceMark = null;

					var delta = tuple.Item2;
					var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);
					room.Position = newPos;
				}

				if (!AddRunStep())
				{
					return false;
				}
			} else
			{
				// Remove
				foreach (var room in roomsToDelete)
				{
					room.Position = null;
				}

				// Move
				foreach (var tuple in roomsToMove)
				{
					var room = tuple.Item1;
					var delta = tuple.Item2;

					var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);
					room.Position = newPos;
				}
			}

			return true;
		}


		private StraightenConnectionResult TryStraightenConnection(int sourceRoomId, int targetRoomId, MMBDirection direction)
		{
			var rooms = _rooms.Clone();
			var vc = rooms.BrokenConnections;

			var sourceRoom = rooms.GetRoomById(sourceRoomId);
			var targetRoom = rooms.GetRoomById(targetRoomId);
			var sourcePos = sourceRoom.Position.Value;
			var targetPos = targetRoom.Position.Value;
			var desiredPos = CalculateDesiredPosition(sourcePos, targetPos, direction);
			var delta = new Point(desiredPos.X - targetPos.X, desiredPos.Y - targetPos.Y);

			int roomsRemoved;
			PushRoom(rooms, targetRoomId, delta, true, out roomsRemoved);

			var vc2 = rooms.BrokenConnections;

			return new StraightenConnectionResult(delta, roomsRemoved, vc2);
		}

		private bool CanRoomBeRemoved(MMBRoom room)
		{
			List<RemoveRoomRecord> removes;
			if (!_removalHistory.TryGetValue(room.Id, out removes))
			{
				return true;
			}

			var remove = (from r in removes where r.Position == room.Position select r).FirstOrDefault();

			// Prevent cycles by forbidding to remove rooms twice in same configurations
			return remove == null;
		}

		private bool RemoveRooms(MMBRoom[] rooms)
		{
			// Mark
			foreach (var room in rooms)
			{
				room.MarkColor = SKColors.Red;
			}
			if (!AddRunStep())
			{
				return false;
			}

			// Remove
			foreach (var room in rooms)
			{

				// Record the removal
				List<RemoveRoomRecord> removes;
				if (!_removalHistory.TryGetValue(room.Id, out removes))
				{
					removes = new List<RemoveRoomRecord>();
					_removalHistory[room.Id] = removes;
				}

				var hashCode = _rooms.GetHashCode();
				removes.Add(new RemoveRoomRecord(room.Id, room.Position.Value, hashCode));

				// Remove
				room.MarkColor = null;
				room.Position = null;
				_toProcess.Remove(room);
				_removedRooms.Add(room.Id);
			}

			return AddRunStep();
		}

		private StraightenRoomResult StraightenConnection(MMBRoom room1, MMBRoom room2, MMBDirection direction)
		{
			if (room1.Id == 2960 && room2.Id == 2970)
			{
				var k = 5;
			}

			// Try to move room2
			var vc = _rooms.BrokenConnections;
			var result1 = TryStraightenConnection(room1.Id, room2.Id, direction);
			var result2 = TryStraightenConnection(room2.Id, room1.Id, direction.GetOppositeDirection());

			var vc1 = result1.BrokenConnections;
			var vc2 = result2.BrokenConnections;

			if (vc.NonStraight.Count - vc1.NonStraight.Count <= 0 &&
				vc.NonStraight.Count - vc2.NonStraight.Count <= 0)
			{
				// No connection had been fixed
				return StraightenRoomResult.Fail;
			}

			// Determine which room of two to move
			bool moveSecond;

			// First criteria - amount of non-obstacle connections fixed
			if (vc1.WithObstacles.Count < vc2.WithObstacles.Count)
			{
				moveSecond = true;
			}
			else if (vc1.WithObstacles.Count > vc2.WithObstacles.Count)
			{
				moveSecond = false;
			}
			else
			{
				// Second criteria - amount of non-straight connections fixed
				if (vc1.NonStraight.Count < vc2.NonStraight.Count)
				{
					moveSecond = true;
				}
				else if (vc1.NonStraight.Count > vc2.NonStraight.Count)
				{
					moveSecond = false;
				} else
				{
					// Third criteria - amount of removed rooms
					moveSecond = result1.RoomsRemoved < result2.RoomsRemoved;
				}
			}

			// Do the actual move
			int roomsRemoved;
			if (moveSecond) 
			{
				// Move room2
				if (!PushRoom(_rooms, room2.Id, result1.Delta, false, out roomsRemoved))
				{
					return StraightenRoomResult.OutOfSteps;
				}

				return StraightenRoomResult.Success;
			}

			// Move room1
			if (!PushRoom(_rooms, room1.Id, result2.Delta, false, out roomsRemoved))
			{
				return StraightenRoomResult.OutOfSteps;
			}

			return StraightenRoomResult.Success;
		}

		private MapBuilderResult Process()
		{
			_rooms = new PositionedRooms(_sourceRooms);

			var firstRoom = _rooms.GetRoomById(_sourceRooms[0].Id);
			firstRoom.Position = new Point(0, 0);
			_toProcess.Add(firstRoom);

			while (_toProcess.Count > 0 && _options.MaxSteps > _history.Count)
			{
				while (_toProcess.Count > 0 && _options.MaxSteps > _history.Count)
				{
					var room = _toProcess[0];
					_toProcess.RemoveAt(0);

					foreach (var pair in room.Connections)
					{
						var exitDir = pair.Key;
						if (pair.Value == room.Id)
						{
							continue;
						}

						var newRoom = _rooms.GetRoomById(pair.Value);
						if (newRoom == null || newRoom.Position != null || _toProcess.Contains(newRoom))
						{
							continue;
						}

						var pos = room.Position.Value;
						var delta = exitDir.GetDelta();
						var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);

						var vc = _rooms.BrokenConnections;

						// Expand grid either if the new position is occupied by a room
						// Or if it breaks existing connection
						var expandGrid = false;
						var existingRoom = _rooms.GetRoomByPosition(newPos);
						if (existingRoom != null)
						{
							expandGrid = true;
						}
						else
						{
							//
							var cloneRooms = _rooms.Clone();
							var cloneRoom = cloneRooms.GetRoomById(newRoom.Id);
							cloneRoom.Position = newPos;

							if (cloneRooms.BrokenConnections.WithObstacles.Count > vc.WithObstacles.Count)
							{
								expandGrid = true;
							}
						}

						if (expandGrid)
						{
							// Push rooms in the movement direction
							_rooms.ExpandGrid(newPos, delta);
							if (!AddRunStep())
							{
								goto finish;
							}
						}

						newRoom.Position = newPos;
						_toProcess.Add(newRoom);

						if (!AddRunStep())
						{
							goto finish;
						}

						_rooms.FixPlacementOfSingleExitRooms();

						vc = _rooms.BrokenConnections;

						// Connections fix run
						// Remove obstacles
						while (vc.WithObstacles.Count > 0)
						{
							var wo = vc.WithObstacles[0];

							var roomsToDelete = (from o in wo.Obstacles select _rooms.GetRoomById(o)).ToArray();
							if (!RemoveRooms(roomsToDelete))
							{
								goto finish;
							}

							vc = _rooms.BrokenConnections;
						}

						// Non-straight connections fix
						while(vc.NonStraight.Count > 0)
						{
							// Run while at least one connection could be fixed
							var connectionFixed = false;
							for (var i = 0; i < vc.NonStraight.Count; ++i)
							{
								var ns = vc.NonStraight[i];

								// Try to straighten it
								var room1 = _rooms.GetRoomById(ns.SourceRoomId);
								var room2 = _rooms.GetRoomById(ns.TargetRoomId);
								var srr = StraightenConnection(room1, room2, ns.Direction);
								switch (srr)
								{
									case StraightenRoomResult.Success:
										connectionFixed = true;
										goto connectionFixed;

									case StraightenRoomResult.Fail:
										break;

									case StraightenRoomResult.OutOfSteps:
										goto finish;
								}
							}
						connectionFixed:;
							vc = _rooms.BrokenConnections;

							if (!connectionFixed)
							{
								break;
							}
						}

						if (room.Position == null)
						{
							break;
						}
					}
				}

				if (!AddRunStep())
				{
					break;
				}

				// Update removed rooms
				foreach (var room in _rooms)
				{
					if (room.Position == null)
					{
						continue;
					}

					if (_removedRooms.Contains(room.Id))
					{
						_removedRooms.Remove(room.Id);
					}
				}

				foreach (var roomId in _removedRooms)
				{
					// Find connected room that is processed
					var sourceRoom = _rooms.GetRoomById(roomId);
					foreach (var pair in sourceRoom.Connections)
					{
						var connectedRoom = _rooms.GetRoomById(pair.Value);
						if (connectedRoom == null || connectedRoom.Position == null || _toProcess.Contains(connectedRoom))
						{
							continue;
						}

						_toProcess.Add(connectedRoom);
					}
				}

				// Finally deal with rooms that weren't reached
				foreach (var sourceRoom in _sourceRooms)
				{
					if (_rooms.GetRoomById(sourceRoom.Id) != null ||
						(from tp in _toProcess where tp.Room.Id == sourceRoom.Id select tp).FirstOrDefault() != null ||
						_removedRooms.Contains(sourceRoom.Id))
					{
						// Room had been processed one way or other
						continue;
					}

					// Unprocessed room
					// Firstly check whether it connects to any existing room
				}
			}

		finish:;
			return new MapBuilderResult(_history.ToArray());
		}

		public static MapBuilderResult Build(IMMBRoom[] sourceRooms, BuildOptions options = null)
		{
			var mapBuilder = new MapBuilder(sourceRooms, options);
			return mapBuilder.Process();
		}
	}
}