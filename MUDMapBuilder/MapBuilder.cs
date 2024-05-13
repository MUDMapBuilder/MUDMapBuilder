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
		private readonly RoomsCollection _rooms = new RoomsCollection();
		private readonly List<RoomsCollection> _history = new List<RoomsCollection>();

		private RoomsCollection Rooms => _rooms;

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
			var vc = rooms.Grid.BrokenConnections;

			var sourceRoom = rooms.GetRoomById(sourceRoomId);
			var targetRoom = rooms.GetRoomById(targetRoomId);
			var sourcePos = sourceRoom.Position;
			var targetPos = targetRoom.Position;
			var desiredPos = CalculateDesiredPosition(sourcePos, targetPos, direction);
			var existingRoom = rooms.GetRoomByPosition(desiredPos);
			if (existingRoom != null)
			{
				rooms.Remove(existingRoom.Id);
				roomRemoved = true;
			}

			targetRoom.Position = desiredPos;
			var vc2 = rooms.Grid.BrokenConnections;

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
			if (remove != null)
			{
				var k = 5;
			}
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

			// Remove
			var hashCode = _rooms.GetHashCode();

			_rooms.Remove(room.Id);
			_toProcess.Remove(room);
			_removedRooms.Add(room.Id);

			List<RemoveRoomRecord> removes;
			if (!_removalHistory.TryGetValue(room.Id, out removes))
			{
				removes = new List<RemoveRoomRecord>();
				_removalHistory[room.Id] = removes;
			}

			removes.Add(new RemoveRoomRecord(room.Id, room.Position, hashCode));

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

			var targetPos = room.Position;
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

			if (result1.NonStraightConnectionsFixed > result2.NonStraightConnectionsFixed)
			{
				if (!MoveRoom(room2, result1.DesiredPos))
				{
					return StraightenRoomResult.OutOfSteps;
				}

				return StraightenRoomResult.Success;
			}

			if (!MoveRoom(room1, result2.DesiredPos))
			{
				return StraightenRoomResult.OutOfSteps;
			}

			return StraightenRoomResult.Success;
		}

		private int CalculateAccessibleRooms()
		{
			var result = 0;
			var toProcess = new List<int>();
			var processed = new HashSet<int>();

			toProcess.Add(_sourceRooms[0].Id);

			while (toProcess.Count > 0)
			{
				var roomId = toProcess[0];
				toProcess.RemoveAt(0);
				processed.Add(roomId);
				++result;

				var room = (from r in _sourceRooms where r.Id == roomId select r).First();
				foreach (var pair in room.Exits)
				{
					var exitDir = pair.Key;
					var exitRoom = pair.Value;
					if (processed.Contains(exitRoom.Id) || toProcess.Contains(exitRoom.Id))
					{
						continue;
					}

					toProcess.Add(exitRoom.Id);
				}
			}

			return result;
		}

		private MapBuilderResult Process()
		{
			// First run: calculate accessible rooms
			var totalRooms = CalculateAccessibleRooms();

			// Now do the actual placement
			var firstRoom = new MMBRoom(_sourceRooms[0])
			{
				Position = new Point(0, 0)
			};
			_toProcess.Add(firstRoom);
			_rooms.Add(firstRoom);

			while (_toProcess.Count > 0 && _options.MaxSteps > _history.Count)
			{
				while (_toProcess.Count > 0 && _options.MaxSteps > _history.Count)
				{
					var room = _toProcess[0];
					_toProcess.RemoveAt(0);

					foreach (var pair in room.Room.Exits)
					{
						var exitDir = pair.Key;
						var exitRoom = pair.Value;
						if (_rooms.GetRoomById(exitRoom.Id) != null)
						{
							continue;
						}

						var pos = room.Position;
						var delta = exitDir.GetDelta();
						var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);

						var vc = _rooms.Grid.BrokenConnections;

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
							var cloneRoom = new MMBRoom(exitRoom)
							{
								Position = newPos
							};

							cloneRooms.Add(cloneRoom);
							if (cloneRooms.Grid.BrokenConnections.WithObstacles.Count > vc.WithObstacles.Count)
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

						var newRoom = new MMBRoom(exitRoom)
						{
							Position = newPos
						};
						_toProcess.Add(newRoom);
						_rooms.Add(newRoom);

						if (!AddRunStep())
						{
							goto finish;
						}

						_rooms.FixPlacementOfSingleExitRooms();

						vc = _rooms.BrokenConnections;

						// Connections fix run
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
					}
				}

				if (!AddRunStep())
				{
					break;
				}

				// Update removed rooms
				foreach (var room in _rooms)
				{
					if (_removedRooms.Contains(room.Id))
					{
						_removedRooms.Remove(room.Id);
					}
				}

				foreach (var roomId in _removedRooms)
				{
					// Find connected room that is processed
					var sourceRoom = (from s in _sourceRooms where s.Id == roomId select s).First();
					foreach (var pair in sourceRoom.Exits)
					{
						var exitRoom = pair.Value;
						var connectedRoom = _rooms.GetRoomById(exitRoom.Id);
						if (connectedRoom == null || _toProcess.Contains(connectedRoom))
						{
							continue;
						}

						_toProcess.Add(connectedRoom);
					}
				}
			}

		finish:;
			return new MapBuilderResult(_history.ToArray(), totalRooms);
		}

		public static MapBuilderResult Build(IMMBRoom[] sourceRooms, BuildOptions options = null)
		{
			var mapBuilder = new MapBuilder(sourceRooms, options);
			return mapBuilder.Process();
		}
	}
}