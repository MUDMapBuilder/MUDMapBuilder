using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	public partial class PositionedRooms : IEnumerable<MMBRoom>
	{
		private enum ConnectionBrokenType
		{
			Normal,
			NotStraight,
			HasObstacles,
			Long
		}

		private readonly Dictionary<int, MMBRoom> _roomsByIds = new Dictionary<int, MMBRoom>();
		private Rectangle _roomsRectangle;
		private MMBRoom[,] _roomsByPositions = null;
		private MMBConnectionsList[,] _connectionsGrid = null;
		private BrokenConnectionsInfo _brokenConnections;
		private int _gridArea;

		public int Count => _roomsByIds.Count;
		public int PositionedRoomsCount => (from r in _roomsByIds.Values where r.Position != null select r).Count();

		public BrokenConnectionsInfo BrokenConnections
		{
			get
			{
				UpdatePositions();
				return _brokenConnections;
			}
		}

		public Rectangle RoomsRectangle
		{
			get
			{
				UpdatePositions();
				return _roomsRectangle;
			}
		}

		public int GridArea => _gridArea;

		public int Width => RoomsRectangle.Width;
		public int Height => RoomsRectangle.Height;

		public int? SelectedRoomId { get; set; }

		private PositionedRooms()
		{
		}

		internal PositionedRooms(IMMBRoom[] sourceRooms)
		{
			// Create rooms
			foreach (var r in sourceRooms)
			{
				Add(new MMBRoom(r));
			}

			// Set connections
			foreach (var pair in _roomsByIds)
			{
				var room = pair.Value;
				foreach (var exit in pair.Value.Room.Exits)
				{
					var targetRoom = GetRoomById(exit.Value.Id);
					if (targetRoom == null)
					{
						continue;
					}

					var exitDir = exit.Key;
					room.Connections[exitDir] = exit.Value.Id;

					var foundOpposite = false;
					foreach (var targetRoomExit in targetRoom.Room.Exits)
					{
						if (targetRoomExit.Value.Id == room.Id)
						{
							foundOpposite = true;
							break;
						}
					}

					if (!foundOpposite)
					{
						targetRoom.Connections[exitDir.GetOppositeDirection()] = room.Id;
					}
				}
			}
		}

		private void OnRoomInvalid(object sender, EventArgs e) => InvalidatePositions();

		private void Add(MMBRoom room)
		{
			room.Invalid += OnRoomInvalid;

			_roomsByIds[room.Id] = room;
			InvalidatePositions();
		}

		public void InvalidatePositions()
		{
			_roomsByPositions = null;
			_brokenConnections = null;
			_connectionsGrid = null;
		}

		public void ExpandGrid(Point pos, Point vec)
		{
			foreach (var pair in _roomsByIds)
			{
				var room = pair.Value;
				if (room.Position == null)
				{
					continue;
				}

				var roomPos = room.Position.Value;
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

		public MMBRoom GetRoomById(int id)
		{
			MMBRoom result;
			if (!_roomsByIds.TryGetValue(id, out result))
			{
				return null;
			}

			return result;
		}

		public Point ToZeroBasedPosition(Point pos)
		{
			pos.X -= RoomsRectangle.X;
			pos.Y -= RoomsRectangle.Y;

			return pos;
		}

		public MMBRoom GetRoomByZeroBasedPosition(int x, int y)
		{
			UpdatePositions();

			if (x < 0 || x >= Width ||
				y < 0 || y >= Height)
			{
				return null;
			}

			return _roomsByPositions[x, y];
		}

		public MMBRoom GetRoomByZeroBasedPosition(Point pos) => GetRoomByZeroBasedPosition(pos.X, pos.Y);

		public MMBRoom GetRoomByPosition(int x, int y)
		{
			x -= RoomsRectangle.X;
			y -= RoomsRectangle.Y;

			return GetRoomByZeroBasedPosition(x, y);
		}

		public MMBRoom GetRoomByPosition(Point pos) => GetRoomByPosition(pos.X, pos.Y);

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
					isStraight = targetPos.X > sourcePos.X && targetPos.Y < sourcePos.Y;
					break;

				case MMBDirection.Down:
					isStraight = targetPos.X < sourcePos.X && targetPos.Y > sourcePos.Y;
					break;
			}

			return isStraight;
		}

		private ConnectionBrokenType CheckConnectionBroken(MMBRoom sourceRoom, MMBRoom targetRoom, MMBDirection exitDir, out HashSet<int> obstacles)
		{
			var sourcePos = sourceRoom.Position.Value;
			var targetPos = targetRoom.Position.Value;

			obstacles = new HashSet<int>();

			var delta = exitDir.GetDelta();
			var desiredPos = new Point(sourcePos.X + delta.X, sourcePos.Y + delta.Y);
			if (desiredPos == targetPos)
			{
				return ConnectionBrokenType.Normal;
			}

			var isStraight = IsConnectionStraight(sourcePos, targetPos, exitDir);
			if (!isStraight)
			{
				// Even non-straight connection is considered straight
				// If there's another straight connection between same rooms
				return ConnectionBrokenType.NotStraight;
			}
			else
			{
				if (exitDir == MMBDirection.Up || exitDir == MMBDirection.Down)
				{
					return ConnectionBrokenType.Long;
				}

				// Check there are no obstacles on the path
				var startCheck = sourcePos;
				var endCheck = targetPos;
				switch (exitDir)
				{
					case MMBDirection.North:
						startCheck = new Point(sourcePos.X, sourcePos.Y - 1);
						endCheck = new Point(targetPos.X, targetPos.Y + 1);
						break;
					case MMBDirection.East:
						startCheck = new Point(sourcePos.X + 1, sourcePos.Y);
						endCheck = new Point(targetPos.X - 1, targetPos.Y);
						break;
					case MMBDirection.South:
						startCheck = new Point(sourcePos.X, sourcePos.Y + 1);
						endCheck = new Point(targetPos.X, targetPos.Y - 1);
						break;
					case MMBDirection.West:
						startCheck = new Point(sourcePos.X - 1, sourcePos.Y);
						endCheck = new Point(targetPos.X + 1, targetPos.Y);
						break;
					case MMBDirection.Up:
						startCheck = new Point(sourcePos.X + 1, sourcePos.Y - 1);
						endCheck = new Point(targetPos.X - 1, targetPos.Y + 1);
						break;
					case MMBDirection.Down:
						startCheck = new Point(sourcePos.X - 1, sourcePos.Y + 1);
						endCheck = new Point(targetPos.X + 1, targetPos.Y - 1);
						break;
				}

				for (var x = Math.Min(startCheck.X, endCheck.X); x <= Math.Max(startCheck.X, endCheck.X); ++x)
				{
					for (var y = Math.Min(startCheck.Y, endCheck.Y); y <= Math.Max(startCheck.Y, endCheck.Y); ++y)
					{
						var pos = new Point(x, y);
						if (pos == sourcePos || pos == targetPos)
						{
							continue;
						}

						var room = GetRoomByPosition(pos);
						if (room != null)
						{
							obstacles.Add(room.Room.Id);
						}

						var gridPos = ToZeroBasedPosition(new Point(x, y));
						MMBConnectionsList connections = _connectionsGrid[gridPos.X, gridPos.Y];
						if (connections == null)
						{
							connections = new MMBConnectionsList();
							_connectionsGrid[gridPos.X, gridPos.Y] = connections;
						}

						connections.Add(sourceRoom.Id, targetRoom.Id, exitDir);
					}
				}

				if (obstacles.Count > 0)
				{
					return ConnectionBrokenType.HasObstacles;
				}
			}

			return ConnectionBrokenType.Long;
		}

		private BrokenConnectionsInfo CalculateBrokenConnections()
		{
			var result = new BrokenConnectionsInfo();

			foreach (var ri in _roomsByIds)
			{
				var room = ri.Value;
				if (room.Position == null)
				{
					continue;
				}

				var pos = room.Position.Value;
				foreach (var pair in room.Connections)
				{
					var exitDir = pair.Key;
					if (pair.Value == room.Id)
					{
						continue;
					}

					var targetRoom = GetRoomById(pair.Value);
					if (targetRoom == null || targetRoom.Position == null)
					{
						continue;
					}

					HashSet<int> obstacles;
					var brokenType = CheckConnectionBroken(room, targetRoom, exitDir, out obstacles);
					switch (brokenType)
					{
						case ConnectionBrokenType.Normal:
							result.Normal.Add(room.Id, targetRoom.Id, exitDir);
							break;
						case ConnectionBrokenType.NotStraight:
							result.NonStraight.Add(room.Id, targetRoom.Id, exitDir);
							break;
						case ConnectionBrokenType.HasObstacles:
							var conn = result.WithObstacles.Add(room.Id, targetRoom.Id, exitDir);
							foreach (var o in obstacles)
							{
								conn.Obstacles.Add(o);
							}
							break;
						case ConnectionBrokenType.Long:
							result.Long.Add(room.Id, targetRoom.Id, exitDir);
							break;
					}
				}
			}

			var toDelete = new List<MMBConnection>();
			foreach (var vs in result.NonStraight)
			{
				if (result.Normal.Find(vs.SourceRoomId, vs.TargetRoomId) != null ||
					result.Long.Find(vs.SourceRoomId, vs.TargetRoomId) != null)
				{
					toDelete.Add(vs);
				}
			}

			foreach (var vs in toDelete)
			{
				result.NonStraight.Remove(vs);
			}

			// Intersections
			for (var x = 0; x < Width; ++x)
			{
				for (var y = 0; y < Height; ++y)
				{
					var connections = _connectionsGrid[x, y];
					if (connections == null || connections.Count <= 1)
					{
						continue;
					}

					foreach (var c in connections)
					{
						result.Intersections.Add(c.SourceRoomId, c.TargetRoomId, c.Direction);
					}
				}
			}

			return result;
		}

		private Rectangle CalculateRectangle()
		{
			var min = new Point();
			var max = new Point();
			var minSet = false;
			var maxSet = false;

			foreach (var ri in _roomsByIds)
			{
				var room = ri.Value;
				if (room.Position == null)
				{
					continue;
				}

				var pos = room.Position.Value;
				if (!minSet)
				{
					min = pos;
					minSet = true;
				}
				else
				{
					if (pos.X < min.X)
					{
						min.X = pos.X;
					}

					if (pos.Y < min.Y)
					{
						min.Y = pos.Y;
					}
				}

				if (!maxSet)
				{
					max = pos;
					maxSet = true;
				}
				else
				{
					if (pos.X > max.X)
					{
						max.X = pos.X;
					}

					if (pos.Y > max.Y)
					{
						max.Y = pos.Y;
					}
				}
			}

			return new Rectangle(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
		}

		private void UpdatePositions()
		{
			if (_roomsByPositions != null)
			{
				return;
			}

			_roomsRectangle = CalculateRectangle();

			_roomsByPositions = new MMBRoom[_roomsRectangle.Width, _roomsRectangle.Height];
			_connectionsGrid = new MMBConnectionsList[_roomsRectangle.Width, _roomsRectangle.Height];

			// Add rooms
			foreach (var ri in _roomsByIds)
			{
				var room = ri.Value;
				if (room.Position == null)
				{
					continue;
				}

				var pos = room.Position.Value;
				var coord = new Point(pos.X - _roomsRectangle.X, pos.Y - _roomsRectangle.Y);
				_roomsByPositions[coord.X, coord.Y] = room;
			}

			_brokenConnections = CalculateBrokenConnections();

			// Calculate grid area
			_gridArea = 0;
			for (var y = 0; y < Height; ++y)
			{
				int startX, endX;
				var found = false;
				for (startX = 0; startX < Width; ++startX)
				{
					var room = GetRoomByZeroBasedPosition(startX, y);
					if (room != null)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					// Skip this line
					continue;
				}

				for (endX = Width - 1; endX >= 0; --endX)
				{
					var room = GetRoomByZeroBasedPosition(endX, y);
					if (room != null)
					{
						break;
					}
				}

				if (endX < startX)
				{
					var k = 5;
				}

				_gridArea += (endX - startX + 1);
			}
		}

		public void FixPlacementOfSingleExitRooms()
		{
			foreach (var ri in _roomsByIds)
			{
				var room = ri.Value;
				if (room.Position == null)
				{
					continue;
				}

				var pos = room.Position.Value;
				foreach (var pair in room.Connections)
				{
					var exitDir = pair.Key;
					if (pair.Value == room.Id)
					{
						continue;
					}

					var targetRoom = GetRoomById(pair.Value);
					if (targetRoom == null || targetRoom.Position == null)
					{
						continue;
					}

					if (targetRoom.Connections.Count > 1)
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
		}

		public PositionedRooms Clone()
		{
			var result = new PositionedRooms();
			foreach (var r in _roomsByIds)
			{
				result.Add(r.Value.Clone());
			}

			return result;
		}

		public MeasurePushRoomResult MeasurePushRoom(int firstRoomId, Point firstForceVector)
		{
			// Determine rooms movement
			var movedRooms = new Dictionary<int, Point>();

			var firstRoom = GetRoomById(firstRoomId);
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

				movedRooms[room.Id] = item.Item2;

				// Process neighbour rooms
				foreach (var pair in room.Connections)
				{
					var exitDir = pair.Key;
					var forceVector = item.Item2;

					var targetRoom = GetRoomById(pair.Value);
					if (targetRoom == null || targetRoom.Position == null || movedRooms.ContainsKey(pair.Value))
					{
						continue;
					}

					if (!IsConnectionStraight(room.Position.Value, targetRoom.Position.Value, exitDir))
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

			// Determine deleted rooms
			var movedRoomsList = new List<MeasurePushRoomMovement>();
			var deletedRooms = new List<MMBRoom>();
			foreach (var pair in movedRooms)
			{
				var room = GetRoomById(pair.Key);
				var delta = pair.Value;

				movedRoomsList.Add(new MeasurePushRoomMovement(room, delta));

				var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);

				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !movedRooms.ContainsKey(existingRoom.Id))
				{
					deletedRooms.Add(existingRoom);
				}
			}

			return new MeasurePushRoomResult(movedRoomsList.ToArray(), deletedRooms.ToArray());
		}

		private void MeasureCompactPushAddConnectionNeighbours(MMBDirection pushDirection, MMBRoom sourceRoom, MMBRoom targetRoom, MMBDirection exitDir, HashSet<int> movedRooms, List<MMBRoom> toProcess)
		{
			var doCheck = ((pushDirection == MMBDirection.North || pushDirection == MMBDirection.South) &&
						(exitDir == MMBDirection.West || exitDir == MMBDirection.East)) ||
						((pushDirection == MMBDirection.West || pushDirection == MMBDirection.East) &&
						(exitDir == MMBDirection.North || exitDir == MMBDirection.South));

			if (!doCheck)
			{
				return;
			}

			var sourcePos = sourceRoom.Position.Value;
			var targetPos = targetRoom.Position.Value;

			if (exitDir == MMBDirection.West || exitDir == MMBDirection.East)
			{
				// Horizontal movement
				int startX, endX;
				if (exitDir == MMBDirection.West)
				{
					startX = sourcePos.X - 1;
					endX = targetPos.X + 1;
				}
				else
				{
					startX = sourcePos.X + 1;
					endX = targetPos.X - 1;
				}

				int y;
				if (pushDirection == MMBDirection.North)
				{
					y = sourcePos.Y - 1;
				}
				else
				{
					y = sourcePos.Y + 1;
				}

				for (var x = startX; x <= endX; x++)
				{
					var neighbourRoom = GetRoomByPosition(new Point(x, y));
					if (neighbourRoom != null && !movedRooms.Contains(neighbourRoom.Id))
					{
						toProcess.Add(neighbourRoom);
					}
				}
			}
			else
			{
				// Vertical movement

				int startY, endY;
				if (exitDir == MMBDirection.North)
				{
					startY = sourcePos.Y - 1;
					endY = targetPos.Y + 1;
				}
				else
				{
					startY = sourcePos.Y + 1;
					endY = targetPos.Y - 1;
				}

				int x;
				if (pushDirection == MMBDirection.West)
				{
					x = sourcePos.X - 1;
				}
				else
				{
					x = sourcePos.X + 1;
				}

				for (var y = startY; y <= endY; y++)
				{
					var neighbourRoom = GetRoomByPosition(new Point(x, y));
					if (neighbourRoom != null && !movedRooms.Contains(neighbourRoom.Id))
					{
						toProcess.Add(neighbourRoom);
					}
				}
			}
		}

		public MeasurePushRoomResult MeasureCompactPushRoom(int firstRoomId, MMBDirection direction)
		{
			// Determine rooms movement
			var delta = direction.GetDelta();
			var movedRooms = new HashSet<int>();
			var firstRoom = GetRoomById(firstRoomId);
			var toProcess = new List<MMBRoom>
			{
				firstRoom
			};

			while (toProcess.Count > 0)
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);

				movedRooms.Add(room.Id);

				// Process connected rooms
				var sourcePos = room.Position.Value;
				foreach (var pair in room.Connections)
				{
					var exitDir = pair.Key;
					var targetRoom = GetRoomById(pair.Value);
					if (targetRoom == null || targetRoom.Position == null || movedRooms.Contains(pair.Value))
					{
						continue;
					}

					if (!IsConnectionStraight(room.Position.Value, targetRoom.Position.Value, exitDir))
					{
						// Skip broken connections
						continue;
					}

					var targetPos = targetRoom.Position.Value;

					var doAdd = false;
					switch (direction)
					{
						case MMBDirection.North:
							doAdd = exitDir == MMBDirection.West || exitDir == MMBDirection.East || exitDir == MMBDirection.South || exitDir == MMBDirection.Down ||
								(exitDir == MMBDirection.Up && sourcePos.Y - targetPos.Y == 1);
							break;
						case MMBDirection.East:
							doAdd = exitDir == MMBDirection.North || exitDir == MMBDirection.South || exitDir == MMBDirection.West || exitDir == MMBDirection.Down ||
								(exitDir == MMBDirection.Up && sourcePos.X - targetPos.X == -1);
							break;
						case MMBDirection.South:
							doAdd = exitDir == MMBDirection.West || exitDir == MMBDirection.East || exitDir == MMBDirection.North || exitDir == MMBDirection.Up ||
								(exitDir == MMBDirection.Down && sourcePos.Y - targetPos.Y == -1);
							break;
						case MMBDirection.West:
							doAdd = exitDir == MMBDirection.North || exitDir == MMBDirection.South || exitDir == MMBDirection.East || exitDir == MMBDirection.Up ||
								(exitDir == MMBDirection.Down && sourcePos.X - targetPos.X == 1);
							break;
					}

					if (doAdd)
					{
						toProcess.Add(targetRoom);
					}

					// Add rooms neighbor to connection
					MeasureCompactPushAddConnectionNeighbours(direction, room, targetRoom, exitDir, movedRooms, toProcess);
				}

				// Add neighbor room
				var neighbourPos = new Point(sourcePos.X + delta.X, sourcePos.Y + delta.Y);
				var neighbourRoom = GetRoomByPosition(neighbourPos);
				if (neighbourRoom != null && !movedRooms.Contains(neighbourRoom.Id))
				{
					toProcess.Add(neighbourRoom);
				}
			}

			// Determine deleted rooms
			var movedRoomsList = new List<MeasurePushRoomMovement>();
			var deletedRooms = new List<MMBRoom>();
			foreach (var pair in movedRooms)
			{
				var room = GetRoomById(pair);

				movedRoomsList.Add(new MeasurePushRoomMovement(room, delta));

				var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);

				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !movedRooms.Contains(existingRoom.Id))
				{
					deletedRooms.Add(existingRoom);
				}
			}

			return new MeasurePushRoomResult(movedRoomsList.ToArray(), deletedRooms.ToArray());
		}

		public void ClearMarks()
		{
			foreach (var pair in _roomsByIds)
			{
				var room = pair.Value;
				room.MarkColor = null;
				room.ForceMark = null;
			}
		}

		public IEnumerator<MMBRoom> GetEnumerator() => _roomsByIds.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _roomsByIds.Values.GetEnumerator();

		public static bool AreEqual(PositionedRooms a, PositionedRooms b)
		{
			if (a.Width != b.Width || a.Height != b.Height)
			{
				return false;
			}

			for (var x = 0; x < a.Width; ++x)
			{
				for (var y = 0; y < a.Height; ++y)
				{
					var roomA = a.GetRoomByZeroBasedPosition(x, y);
					var roomB = b.GetRoomByZeroBasedPosition(x, y);

					if (roomA == null && roomB == null)
					{
						continue;
					}

					if ((roomA == null && roomB != null) ||
						(roomA != null && roomB == null))
					{
						return false;
					}

					if (roomA.Id != roomB.Id)
					{
						return false;
					}
				}
			}

			return true;
		}
	}
}