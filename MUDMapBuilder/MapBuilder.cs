using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		private readonly IMMBRoom[] _sourceRooms;
		private readonly BuildOptions _options;
		private readonly List<MMBRoom> _toProcess = new List<MMBRoom>();
		private readonly HashSet<int> _removedRooms = new HashSet<int>();
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

		private bool PushRoom(PositionedRooms rooms, int firstRoomId, Point firstForceVector, bool measureRun, out int roomsRemoved)
		{
			var measure = rooms.MeasurePushRoom(firstRoomId, firstForceVector);

			roomsRemoved = measure.DeletedRooms.Length;

			if (!measureRun)
			{
				if (measure.DeletedRooms.Length > 0)
				{
					return RemoveRooms(measure.DeletedRooms);
				}

				// Mark for movement
				foreach (var m in measure.MovedRooms)
				{
					var room = m.Room;
					room.MarkColor = SKColors.YellowGreen;
					room.ForceMark = m.Delta;
				}

				if (!AddRunStep())
				{
					return false;
				}

				// Do the movement
				foreach (var m in measure.MovedRooms)
				{
					var room = m.Room;
					room.MarkColor = null;
					room.ForceMark = null;

					var delta = m.Delta;
					var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);
					room.Position = newPos;
				}

				if (!AddRunStep())
				{
					return false;
				}
			}
			else
			{
				// Remove
				foreach (var room in measure.DeletedRooms)
				{
					room.Position = null;
				}

				// Move
				foreach (var m in measure.MovedRooms)
				{
					var room = m.Room;
					var delta = m.Delta;

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

		private bool RemoveRooms(MMBRoom[] toRemove)
		{
			// Firstly remove from the clone in order to determine how rooms will be spliited
			var rooms = _rooms.Clone();
			foreach (var r in toRemove)
			{
				var cloneRoom = rooms.GetRoomById(r.Id);
				cloneRoom.Position = null;
			}

			// Check what parts the map was split on
			var parts = new List<HashSet<int>>();
			foreach (var room in rooms)
			{
				if (room.Position == null)
				{
					continue;
				}

				// Check if this room is already in one of parts
				foreach (var p in parts)
				{
					if (p.Contains(room.Id))
					{
						goto finish;
					}
				}

				// Create new part with this room and all its connections
				var newPart = new HashSet<int>();
				var toProcess = new List<MMBRoom>
				{
					room
				};

				while (toProcess.Count > 0)
				{
					var r = toProcess[0];
					toProcess.RemoveAt(0);
					newPart.Add(r.Id);

					foreach (var exit in r.Connections)
					{
						if (newPart.Contains(exit.Value))
						{
							continue;
						}

						var targetRoom = rooms.GetRoomById(exit.Value);
						if (targetRoom == null || targetRoom.Position == null)
						{
							continue;
						}

						toProcess.Add(targetRoom);
					}
				}

				parts.Add(newPart);
			finish:;
			}

			var roomsToRemove = new List<MMBRoom>();
			roomsToRemove.AddRange(toRemove);

			// Sort parts by size
			var sortedParts = (from p in parts orderby p.Count select p).ToList();

			// Add all parts except the last one with size below 10
			for (var i = 0; i < sortedParts.Count - 1; ++i)
			{
				var p = sortedParts[i];
				if (p.Count >= 10)
				{
					continue;
				}

				foreach (var id in p)
				{
					roomsToRemove.Add(_rooms.GetRoomById(id));
				}
			}

			// Mark
			foreach (var room in roomsToRemove)
			{
				room.MarkColor = SKColors.Red;
			}

			if (!AddRunStep())
			{
				return false;
			}

			// Remove
			foreach (var room in roomsToRemove)
			{
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
				}
				else
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

		private bool CompactRun(MMBDirection pushDirection)
		{
			// Firstly collect rooms
			var roomsToPush = new List<MMBRoom>();
			if (pushDirection == MMBDirection.West || pushDirection == MMBDirection.East)
			{
				for (var y = 0; y < _rooms.Height; ++y)
				{
					if (pushDirection == MMBDirection.West)
					{
						for (var x = _rooms.Width - 1; x >= 0; --x)
						{
							var room = _rooms.GetRoomByZeroBasedPosition(x, y);
							if (room != null)
							{
								roomsToPush.Add(room);
								break;
							}
						}
					}
					else
					{
						for (var x = 0; x < _rooms.Width; ++x)
						{
							var room = _rooms.GetRoomByZeroBasedPosition(x, y);
							if (room != null)
							{
								roomsToPush.Add(room);
								break;
							}
						}
					}
				}
			}
			else
			{
				for (var x = 0; x < _rooms.Width; ++x)
				{
					if (pushDirection == MMBDirection.North)
					{
						for (var y = _rooms.Height - 1; y >= 0; --y)
						{
							var room = _rooms.GetRoomByZeroBasedPosition(x, y);
							if (room != null)
							{
								roomsToPush.Add(room);
								break;
							}
						}
					}
					else
					{
						for (var y = 0; y < _rooms.Height; ++y)
						{
							var room = _rooms.GetRoomByZeroBasedPosition(x, y);
							if (room != null)
							{
								roomsToPush.Add(room);
								break;
							}
						}
					}
				}
			}

			// Now try to push every room in the provided direction until it's possible
			foreach (var room in roomsToPush)
			{
				var continueToPush = true;
				while (continueToPush)
				{
					continueToPush = false;
					var measure = _rooms.MeasureCompactPushRoom(room.Id, pushDirection);
					if (measure.DeletedRooms.Length > 0)
					{
						// Should never happen
						Debug.Assert(false);
					}
					else
					{
						// Test push
						var vc = _rooms.BrokenConnections;
						var rooms = _rooms.Clone();

						foreach (var m in measure.MovedRooms)
						{
							var newPos = new Point(m.Room.Position.Value.X + m.Delta.X,
								m.Room.Position.Value.Y + m.Delta.Y);

							var roomClone = rooms.GetRoomById(m.Room.Id);
							roomClone.Position = newPos;
						}

						var vc2 = rooms.BrokenConnections;
						if (vc2.WithObstacles.Count > vc.WithObstacles.Count ||
							vc2.NonStraight.Count > vc.NonStraight.Count)
						{
							// Such push would break some room connections
						}
						else if (rooms.Width * rooms.Height > _rooms.Width * _rooms.Height)
						{
							// Such push would make the grid bigger
						}
						else if (PositionedRooms.AreEqual(rooms, _rooms))
						{
							// Such push wouldn't change anything
						}
						else
						{
							// Mark the movement
							foreach (var m in measure.MovedRooms)
							{
								m.Room.ForceMark = m.Delta;
							}

							if (!AddRunStep())
							{
								return false;
							}

							// Do the move
							_rooms.ClearMarks();
							foreach (var m in measure.MovedRooms)
							{
								var newPos = new Point(m.Room.Position.Value.X + m.Delta.X,
									m.Room.Position.Value.Y + m.Delta.Y);

								m.Room.Position = newPos;
							}

							if (!AddRunStep())
							{
								return false;
							}

							continueToPush = true;
						}
					}
				}
			}

			_rooms.FixPlacementOfSingleExitRooms();

			return true;
		}

		private MapBuilderResult Process()
		{
			_rooms = new PositionedRooms(_sourceRooms);

			var firstRoom = _rooms.GetRoomById(_sourceRooms[0].Id);
			firstRoom.Position = new Point(0, 0);
			_toProcess.Add(firstRoom);

			BrokenConnectionsInfo vc;
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

						vc = _rooms.BrokenConnections;

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
						while (vc.NonStraight.Count > 0)
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
			var startCompactStep = _history.Count;
			if (!CompactRun(MMBDirection.East))
			{
				goto finish2;
			}

			if (!CompactRun(MMBDirection.South))
			{
				goto finish2;
			}
			if (!CompactRun(MMBDirection.West))
			{
				goto finish2;
			}

			if (!CompactRun(MMBDirection.North))
			{
				goto finish2;
			}

		finish2:;
			return new MapBuilderResult(_history.ToArray(), startCompactStep);
		}

		public static MapBuilderResult Build(IMMBRoom[] sourceRooms, BuildOptions options = null)
		{
			var mapBuilder = new MapBuilder(sourceRooms, options);
			return mapBuilder.Process();
		}
	}
}