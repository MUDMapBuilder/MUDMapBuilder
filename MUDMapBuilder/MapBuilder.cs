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
			public Point DesiredPos { get; private set; }
			public bool RoomRemoved { get; private set; }
			public int NonStraightConnectionsFixed { get; private set; }

			public StraightenConnectionResult(Point desiredPos, bool roomRemoved, int nonStraightConnectionsFixed)
			{
				DesiredPos = desiredPos;
				RoomRemoved = roomRemoved;
				NonStraightConnectionsFixed = nonStraightConnectionsFixed;
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

		private StraightenConnectionResult TryStraightenConnection(int sourceRoomId, int targetRoomId, MMBDirection direction)
		{
			var roomRemoved = false;
			var rooms = _rooms.Clone();
			var vc = rooms.BrokenConnections;

			var sourceRoom = rooms.GetRoomById(sourceRoomId);
			var targetRoom = rooms.GetRoomById(targetRoomId);
			var sourcePos = sourceRoom.Position.Value;
			var targetPos = targetRoom.Position.Value;
			var desiredPos = CalculateDesiredPosition(sourcePos, targetPos, direction);
			var existingRoom = rooms.GetRoomByPosition(desiredPos);
			if (existingRoom != null)
			{
				existingRoom.Position = null;
				roomRemoved = true;
			}

			targetRoom.Position = desiredPos;
			var vc2 = rooms.BrokenConnections;

			var connectionsFixed = vc.NonStraight.Count - vc2.NonStraight.Count;

			return new StraightenConnectionResult(desiredPos, roomRemoved, connectionsFixed);
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

		private bool RemoveRoom(MMBRoom room)
		{
			if (room == null)
			{
				return true;
			}

			// Mark
			room.MarkColor = SKColors.Red;
			if (!AddRunStep())
			{
				return false;
			}

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

			return AddRunStep();
		}

		private bool MoveRoom(MMBRoom room, Point desiredPos)
		{
			var existingRoom = _rooms.GetRoomByPosition(desiredPos);
			if (existingRoom != null)
			{
				if (!RemoveRoom(existingRoom))
				{
					return false;
				}
			}

			var targetPos = room.Position.Value;
			var delta = new Point(desiredPos.X - targetPos.X, desiredPos.Y - targetPos.Y);
			room.ForceMark = delta;
			if (!AddRunStep())
			{
				return false;
			}

			room.ForceMark = null;
			room.Position = desiredPos;
			if (!AddRunStep())
			{
				return false;
			}

			return true;
		}

		private StraightenRoomResult StraightenConnection(MMBRoom room1, MMBRoom room2, MMBDirection direction)
		{
			// Try to move room2
			var result1 = TryStraightenConnection(room1.Id, room2.Id, direction);
			var result2 = TryStraightenConnection(room2.Id, room1.Id, direction.GetOppositeDirection());

			if (result1.NonStraightConnectionsFixed <= 0 && result2.NonStraightConnectionsFixed <= 0)
			{
				return StraightenRoomResult.Fail;
			}

			// Determine which room of two to move
			bool moveSecond;
			if (result1.NonStraightConnectionsFixed > result2.NonStraightConnectionsFixed)
			{
				moveSecond = true;
			} else if (result1.NonStraightConnectionsFixed < result2.NonStraightConnectionsFixed)
			{
				moveSecond = false;
			} else if (!result1.RoomRemoved)
			{
				moveSecond = true;
			} else
			{
				moveSecond = false;
			}

			// Do the actual move
			if (moveSecond) 
			{
				// Move room2
				if (!MoveRoom(room2, result1.DesiredPos))
				{
					return StraightenRoomResult.OutOfSteps;
				}

				return StraightenRoomResult.Success;
			}

			// Move room1
			if (!MoveRoom(room1, result2.DesiredPos))
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
					if (_history.Count >= 500)
					{
						var k = 5;
					}

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

							foreach (var o in wo.Obstacles)
							{
								if (!RemoveRoom(_rooms.GetRoomById(o)))
								{
									goto finish;
								}
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

						// Another non-straight fix run
						// This time removing unfixable rooms
						// Non-straight connections fix
						while (vc.NonStraight.Count > 0)
						{
							var ns = vc.NonStraight[0];
							vc.NonStraight.RemoveAt(0);

							// Try to straighten it
							var room1 = _rooms.GetRoomById(ns.SourceRoomId);
							var room2 = _rooms.GetRoomById(ns.TargetRoomId);
							var srr = StraightenConnection(room1, room2, ns.Direction);
							switch (srr)
							{
								case StraightenRoomResult.Success:
									break;

								case StraightenRoomResult.Fail:
									var roomToRemove = room1;
									if (room1.Id == newRoom.Id || !CanRoomBeRemoved(room1))
									{
										roomToRemove = room2;
									}

									if (CanRoomBeRemoved(roomToRemove) && !RemoveRoom(roomToRemove))
									{
										goto finish;
									}

									break;

								case StraightenRoomResult.OutOfSteps:
									goto finish;
							}

							vc = _rooms.BrokenConnections;
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