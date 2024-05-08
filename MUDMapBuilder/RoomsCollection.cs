using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	internal class RoomsCollection : IEnumerable<MMBRoom>
	{
		public enum ConnectionBrokenType
		{
			NotBroken,
			NotStraight,
			HasObstacles
		}

		public int Count => _rooms.Count;

		public MMBGrid Grid
		{
			get
			{
				UpdateGrid();
				return _grid;
			}
		}

		public MMBRoom this[int index] => _rooms[index];
		private readonly List<MMBRoom> _rooms = new List<MMBRoom>();
		private MMBGrid _grid;
		private Point _min;

		public void Add(MMBRoom room)
		{
			room.Rooms = this;

			_rooms.Add(room);
			InvalidateGrid();
		}

		internal void InvalidateGrid()
		{
			_grid = null;
		}

		private void UpdateGrid()
		{
			if (_grid != null)
			{
				return;
			}

			var rect = CalculateRectangle();

			_min = new Point(rect.X, rect.Y);
			_grid = new MMBGrid(rect.Width, rect.Height);

			// First run: add rooms
			foreach (var room in _rooms)
			{
				var coord = new Point(room.Position.X - rect.X, room.Position.Y - rect.Y);
				_grid[coord.X, coord.Y] = new MMBRoomCell(room.Room, coord);
			}

			// Second run: add connections
			foreach (var room in _rooms)
			{
				var sourceGridRoom = _grid.GetRoomById(room.Id);
				var gridStartPos = sourceGridRoom.Position;

				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					if (exitDir == MMBDirection.Up || exitDir == MMBDirection.Down)
					{
						continue;
					}

					var exitRoom = room.Room.GetRoomByExit(exitDir);
					var targetGridRoom = _grid.GetRoomById(exitRoom.Id);
					if (targetGridRoom == null)
					{
						continue;
					}

					var gridTargetPos = targetGridRoom.Position;
					if (!IsConnectionStraight(gridStartPos, gridTargetPos, exitDir))
					{
						continue;
					}

					var delta = exitDir.GetDelta();
					for (var sourcePos = gridStartPos; sourcePos != gridTargetPos; sourcePos.X += delta.X, sourcePos.Y += delta.Y)
					{
						if (_grid[sourcePos.X, sourcePos.Y] is MMBRoomCell)
						{
							continue;
						}

						var connectionsObstacle = (MMBConnectionsCell)_grid[sourcePos];
						if (connectionsObstacle == null)
						{
							connectionsObstacle = new MMBConnectionsCell(sourcePos);
							_grid[sourcePos] = connectionsObstacle;
						}

						connectionsObstacle.AddPair(sourceGridRoom, targetGridRoom);
					}
				}
			}
		}

		public void ExpandGrid(Point pos, Point vec)
		{
			foreach (var room in _rooms)
			{
				var roomPos = room.Position;
				if (vec.X < 0 && roomPos.X <= pos.X)
				{
					roomPos.X += vec.X;
				}

				if (vec.X > 0 && roomPos.X >= pos.X)
				{
					roomPos.X += vec.X;
				}

				if (vec.Y < 0 && roomPos.Y <= pos.Y)
				{
					roomPos.Y += vec.Y;
				}

				if (vec.Y > 0 && roomPos.Y >= pos.Y)
				{
					roomPos.Y += vec.Y;
				}

				room.Position = roomPos;
			}
		}

		public MMBRoom GetRoomById(int id) => (from r in _rooms where r.Id == id select r).FirstOrDefault();
		public MMBRoom GetRoomByPosition(Point pos) => (from r in _rooms where r.Position == pos select r).FirstOrDefault();

		public void PushRoom(MMBRoom firstRoom, MMBDirection direction, int steps)
		{
			var initialPos = firstRoom.Position;

			// Collect rooms to pull
			var toProcess = new List<MMBRoom>
			{
				firstRoom
			};

			var roomsToPush = new Dictionary<int, MMBRoom>();
			while (toProcess.Count > 0)
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);
				roomsToPush[room.Id] = room;

				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);
					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null || roomsToPush.ContainsKey(targetRoom.Id))
					{
						continue;
					}

					if (!IsConnectionStraight(room.Position, targetRoom.Position, exitDir))
					{
						// Skip broken connections
						continue;
					}

					var targetPos = targetRoom.Position;
					var add = false;
					switch (direction)
					{
						case MMBDirection.North:
							add = targetPos.Y <= initialPos.Y;
							break;
						case MMBDirection.East:
							add = targetPos.X >= initialPos.X;
							break;
						case MMBDirection.South:
							add = targetPos.Y >= initialPos.Y;
							break;
						case MMBDirection.West:
							add = targetPos.X <= initialPos.X;
							break;
						case MMBDirection.Up:
						case MMBDirection.Down:
							throw new NotImplementedException();
					}

					if (add)
					{
						toProcess.Add(targetRoom);
					}
				}
			}

			// Do the push
			var delta = direction.GetDelta();
			delta.X *= steps;
			delta.Y *= steps;
			foreach (var pair in roomsToPush)
			{
				// If there's existing room, then put it on older position
				var room = pair.Value;
				var newPos = new Point(room.Position.X + delta.X, room.Position.Y + delta.Y);

				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !roomsToPush.ContainsKey(existingRoom.Id))
				{
					existingRoom.Position = room.Position;
				}

				room.Position = newPos;
			}
		}

		public static bool IsConnectionStraight(Point sourcePos, Point targetPos, MMBDirection exitDir)
		{
			var isStraight = false;
			switch (exitDir)
			{
				case MMBDirection.North:
					isStraight = targetPos.X - sourcePos.X == 0 && targetPos.Y < sourcePos.Y;
					break;

				case MMBDirection.South:
					isStraight = targetPos.X - sourcePos.X == 0 && targetPos.Y > sourcePos.Y;
					break;

				case MMBDirection.West:
					isStraight = targetPos.X < sourcePos.X && targetPos.Y - sourcePos.Y == 0;
					break;

				case MMBDirection.East:
					isStraight = targetPos.X > sourcePos.X && targetPos.Y - sourcePos.Y == 0;
					break;

				case MMBDirection.Up:
				case MMBDirection.Down:
					isStraight = true;
					break;
			}

			return isStraight;
		}

		public void ApplyForceToRoom(MMBRoom sourceRoom, MMBRoom firstRoom, MMBDirection direction, int steps)
		{
			// First run: determine which rooms to push
			var toProcess = new List<MMBRoom>
			{
				firstRoom
			};

			var roomsToPush = new Dictionary<int, MMBRoom>();

			while (toProcess.Count > 0)
			{
				var room = toProcess[0];
				var pos = room.Position;
				toProcess.RemoveAt(0);

				roomsToPush[room.Id] = room;

				// Process neighbour rooms
				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);

					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null || targetRoom == sourceRoom || roomsToPush.ContainsKey(exitRoom.Id))
					{
						continue;
					}

					if (!IsConnectionStraight(room.Position, targetRoom.Position, exitDir))
					{
						// Skip broken connections
						continue;
					}

					var add = false;
					var targetPos = targetRoom.Position;

					if (Math.Abs(targetPos.X - pos.X) == 1 || Math.Abs(targetPos.Y - pos.Y) == 1)
					{
						// Single size connections are always added
						add = true;
					}
					else if (exitDir == MMBDirection.West || exitDir == MMBDirection.East)
					{
						add = direction == MMBDirection.North || direction == MMBDirection.South;
					}
					else if (exitDir == MMBDirection.North || exitDir == MMBDirection.South)
					{
						add = direction == MMBDirection.West || direction == MMBDirection.East;
					}
					else if (exitDir == MMBDirection.Up || exitDir == MMBDirection.Down)
					{
						add = true;
					}

					if (add)
					{
						toProcess.Add(targetRoom);
					}
				}
			}

			var forceVector = direction.GetDelta();
			forceVector.X *= steps;
			forceVector.Y *= steps;
			foreach (var pair in roomsToPush)
			{
				// If there's existing room, then put it on older position
				var room = pair.Value;
				var newPos = new Point(room.Position.X + forceVector.X, room.Position.Y + forceVector.Y);

				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !roomsToPush.ContainsKey(existingRoom.Id))
				{
					existingRoom.Position = room.Position;
				}

				room.Position = newPos;
			}

			InvalidateGrid();
		}

		private MMBCell GetCell(Point checkPos)
		{
			UpdateGrid();

			checkPos.X -= _min.X;
			checkPos.Y -= _min.Y;

			return _grid[checkPos.X, checkPos.Y];
		}

		public ConnectionBrokenType IsConnectionBroken(MMBRoom sourceRoom, MMBRoom targetRoom, MMBDirection exitDir)
		{
			var pos = sourceRoom.Position;
			var targetPos = targetRoom.Position;
			var isStraight = false;
			switch (exitDir)
			{
				case MMBDirection.North:
					isStraight = targetPos.X - pos.X == 0 && targetPos.Y < pos.Y;
					break;

				case MMBDirection.South:
					isStraight = targetPos.X - pos.X == 0 && targetPos.Y > pos.Y;
					break;

				case MMBDirection.West:
					isStraight = targetPos.X < pos.X && targetPos.Y - pos.Y == 0;
					break;

				case MMBDirection.East:
					isStraight = targetPos.X > pos.X && targetPos.Y - pos.Y == 0;
					break;

				case MMBDirection.Up:
				case MMBDirection.Down:
					// Skip Up/Down for now
					return ConnectionBrokenType.NotBroken;
			}

			if (!isStraight)
			{
				return ConnectionBrokenType.NotStraight;
			}
			else
			{
				// Check there are no obstacles on the path
				var delta = exitDir.GetDelta();
				var p = new Point(pos.X + delta.X, pos.Y + delta.Y);
				var noObstacles = true;
				while (p.X != targetPos.X || p.Y != targetPos.Y)
				{
					var cell = GetCell(p);
					var asRoomCell = cell as MMBRoomCell;
					if (asRoomCell != null)
					{
						noObstacles = false;
						break;
					}

					var asConnectionsCell = cell as MMBConnectionsCell;
					if (asConnectionsCell != null && asConnectionsCell.Count > 1)
					{
						noObstacles = false;
						break;
					}

					p.X += delta.X;
					p.Y += delta.Y;
				}

				if (!noObstacles)
				{
					return ConnectionBrokenType.HasObstacles;
				}
			}

			return ConnectionBrokenType.NotBroken;
		}

		public int CalculateBrokenConnections()
		{
			var result = 0;

			foreach (var room in _rooms)
			{
				var pos = room.Position;
				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);

					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null)
					{
						continue;
					}

					if (IsConnectionBroken(room, targetRoom, exitDir) != ConnectionBrokenType.NotBroken)
					{
						++result;
					}
				}
			}

			return result;
		}

		public Rectangle CalculateRectangle()
		{
			var min = new Point();
			var max = new Point();
			var minSet = false;
			var maxSet = false;

			foreach (var room in _rooms)
			{
				if (!minSet)
				{
					min = room.Position;
					minSet = true;
				}
				else
				{
					if (room.Position.X < min.X)
					{
						min.X = room.Position.X;
					}

					if (room.Position.Y < min.Y)
					{
						min.Y = room.Position.Y;
					}
				}

				if (!maxSet)
				{
					max = room.Position;
					maxSet = true;
				}
				else
				{
					if (room.Position.X > max.X)
					{
						max.X = room.Position.X;
					}

					if (room.Position.Y > max.Y)
					{
						max.Y = room.Position.Y;
					}
				}
			}

			return new Rectangle(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
		}

		public void FixPlacementOfSingleExitRooms()
		{
			foreach (var room in _rooms)
			{
				var pos = room.Position;
				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);

					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null)
					{
						continue;
					}

					if (exitRoom.ExitsDirections.Length > 1)
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

					// Check if the spot is free
					var usedByRoom = GetRoomByPosition(desiredPos);
					if (usedByRoom != null)
					{
						// Spot is occupied
						continue;
					}

					// Place target room next to source
					targetRoom.Position = desiredPos;
				}
			}

			InvalidateGrid();
		}

		public int CalculateArea() => CalculateRectangle().CalculateArea();

		public RoomsCollection Clone()
		{
			var result = new RoomsCollection();
			foreach (var r in _rooms)
			{
				result.Add(r.Clone());
			}

			return result;
		}

		public IEnumerator<MMBRoom> GetEnumerator() => _rooms.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _rooms.GetEnumerator();
	}
}