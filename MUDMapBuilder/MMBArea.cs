using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

namespace MUDMapBuilder
{
	public partial class MMBArea
	{
		public class DeleteColsRowsResult
		{
			public List<int> Columns { get; } = new List<int>();
			public List<int> Rows { get; } = new List<int>();
		}

		private enum ConnectionBrokenType
		{
			Normal,
			NotStraight,
			HasObstacles,
			Long
		}

		private MMBRoom[] _rooms = new MMBRoom[0];
		private readonly Dictionary<int, MMBRoom> _roomsByIds = new Dictionary<int, MMBRoom>();
		private Rectangle _roomsRectangle;
		private MMBRoom[,] _roomsByPositions = null;
		private MMBConnectionsList[,] _connectionsGrid = null;
		private BrokenConnectionsInfo _brokenConnections;

		public string Name { get; set; }
		public string Credits { get; set; }
		public string MinimumLevel { get; set; }
		public string MaximumLevel { get; set; }

		public Color BackgroundColor { get; set; } = Color.White;

		[JsonIgnore]
		public MMBConnectionsList[,] ConnectionsGrid => _connectionsGrid;

		[JsonIgnore]
		public string LogMessage { get; set; }

		public MMBRoom[] Rooms
		{
			get => _rooms;

			set
			{
				if (_rooms != null)
				{
					foreach (var room in _rooms)
					{
						room.RoomInvalid -= OnRoomInvalid;
					}
				}

				_rooms = value;
				_roomsByIds.Clear();

				if (_rooms != null)
				{
					foreach (var room in _rooms)
					{
						room.RoomInvalid += OnRoomInvalid;
						_roomsByIds[room.Id] = room;
					}
				}

				InvalidatePositions();
			}
		}

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

		public int Width => RoomsRectangle.Width;

		public int Height => RoomsRectangle.Height;

		[JsonIgnore]
		public int? SelectedRoomId { get; set; }

		[JsonIgnore]
		public object Tag { get; set; }

		public MMBArea()
		{
		}

		private void OnRoomInvalid(object sender, EventArgs e) => InvalidatePositions();

		public void Add(MMBRoom room)
		{
			// Subscribe
			room.RoomInvalid += OnRoomInvalid;

			// Update internal array
			var list = new List<MMBRoom>();
			if (_rooms != null)
			{
				list.AddRange(_rooms);
			}
			list.Add(room);
			_rooms = list.ToArray();

			// Update internal cache
			_roomsByIds[room.Id] = room;

			// Invalidate positions
			InvalidatePositions();
		}

		public void DeleteRoom(MMBRoom room)
		{
			// Unsubscribe
			room.RoomInvalid -= OnRoomInvalid;

			// Update internal array
			var list = new List<MMBRoom>();
			if (_rooms != null)
			{
				list.AddRange(_rooms);
			}
			list.Remove(room);
			_rooms = list.ToArray();

			// Update internal cache
			_roomsByIds.Remove(room.Id);
			InvalidatePositions();
		}

		public void InvalidatePositions()
		{
			_roomsByPositions = null;
			_brokenConnections = null;
			_connectionsGrid = null;
		}

		public void RemoveNonExistantConnections()
		{
			// Remove exits that lead to nowhere
			foreach (var room in _rooms)
			{
				var toDelete = new List<MMBDirection>();
				foreach (var pair in room.Connections)
				{
					if (!_roomsByIds.ContainsKey(pair.Value.RoomId))
					{
						toDelete.Add(pair.Key);
					}
				}

				foreach (var td in toDelete)
				{
					room.Connections.Remove(td);
				}
			}
		}

		public void RemoveEmptyRooms()
		{
			var toDelete = (from r in _rooms where string.IsNullOrEmpty(r.Name) && r.Connections.Count == 0 select r).ToList();

			foreach (var r in toDelete)
			{
				DeleteRoom(r);
			}
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
				throw new Exception($"Could not find room with id {id}.");
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
							obstacles.Add(room.Id);
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
					// Keep single-exit rooms with the same direction connection
					// As those couldn't be fixed
					var keepRooms = new HashSet<int> { sourceRoom.Id, targetRoom.Id };
					foreach (var o in obstacles)
					{
						if (keepRooms.Contains(o))
						{
							continue;
						}

						var obstacleRoom = GetRoomById(o);
						if (obstacleRoom.Connections.Count != 1)
						{
							continue;
						}

						foreach (var conn in obstacleRoom.Connections)
						{
							var connDir = conn.Value.Direction;
							if ((exitDir.IsHorizontal() && connDir.IsHorizontal()) ||
								(exitDir.IsVertical() && connDir.IsVertical()))
							{
								keepRooms.Add(o);
								break;
							}
						}
					}

					foreach (var k in keepRooms)
					{
						obstacles.Remove(k);
					}

					if (obstacles.Count > 0)
					{
						return ConnectionBrokenType.HasObstacles;
					}
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
					if (pair.Value.RoomId == room.Id)
					{
						continue;
					}

					var targetRoom = GetRoomById(pair.Value.RoomId);
					if (targetRoom.Position == null)
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
			if (PositionedRoomsCount == 0)
			{
				return new Rectangle(0, 0, 0, 0);
			}

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

			_connectionsGrid = new MMBConnectionsList[_roomsRectangle.Width, _roomsRectangle.Height];
			_brokenConnections = CalculateBrokenConnections();
		}

		public DeleteColsRowsResult DeleteEmptyColsRows()
		{
			UpdatePositions();

			var result = new DeleteColsRowsResult();

			// Remove empty columns
			for (var x = 0; x < _roomsRectangle.Width; ++x)
			{
				while (true)
				{
					// Check whether current column is empty
					var isEmpty = true;
					for (var y = 0; y < _roomsRectangle.Height; ++y)
					{
						if (_roomsByPositions[x, y] != null)
						{
							isEmpty = false;
							break;
						}
					}

					if (!isEmpty)
					{
						break;
					}

					result.Columns.Add(x);

					// Delete empty column
					for (var x2 = x; x2 < _roomsRectangle.Width - 1; ++x2)
					{
						for (var y = 0; y < _roomsRectangle.Height; ++y)
						{
							var nextRoom = _roomsByPositions[x2 + 1, y];
							_roomsByPositions[x2, y] = nextRoom;

							if (nextRoom != null)
							{
								// Set field directly to prevent the rebuilding of the grid
								var pos = new Point(x2 + _roomsRectangle.X, y + _roomsRectangle.Y);
								nextRoom._position = pos;
							}
						}
					}

					--_roomsRectangle.Width;
				}
			}

			// Remove empty rows
			for (var y = 0; y < _roomsRectangle.Height; ++y)
			{
				while (true)
				{
					// Check whether current row is empty
					var isEmpty = true;
					for (var x = 0; x < _roomsRectangle.Width; ++x)
					{
						if (_roomsByPositions[x, y] != null)
						{
							isEmpty = false;
							break;
						}
					}

					if (!isEmpty)
					{
						break;
					}

					result.Rows.Add(y);

					// Delete empty row
					for (var y2 = y; y2 < _roomsRectangle.Height - 1; ++y2)
					{
						for (var x = 0; x < _roomsRectangle.Width; ++x)
						{
							var nextRoom = _roomsByPositions[x, y2 + 1];
							_roomsByPositions[x, y2] = nextRoom;

							if (nextRoom != null)
							{
								// Set field directly to prevent the rebuilding of the grid
								var pos = new Point(x + _roomsRectangle.X, y2 + _roomsRectangle.Y);
								nextRoom._position = pos;
							}
						}
					}

					--_roomsRectangle.Height;
				}
			}

			InvalidatePositions();

			return result;
		}

		public HashSet<int>[] GroupPositionedRooms()
		{
			var parts = new List<HashSet<int>>();
			foreach (var ri in _roomsByIds)
			{
				var room = ri.Value;
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
				var processor = new IdQueue(room.Id);
				while (processor.Count > 0)
				{
					var id = processor.Pop();
					var r = GetRoomById(id);

					foreach (var exit in r.Connections)
					{
						var targetRoomId = exit.Value.RoomId;
						if (processor.WasProcessed(targetRoomId))
						{
							continue;
						}

						var targetRoom = GetRoomById(exit.Value.RoomId);
						if (targetRoom.Position == null)
						{
							continue;
						}

						processor.Add(targetRoomId);
					}
				}

				parts.Add(processor.Processed);
			finish:;
			}

			// Sort parts by size
			return (from p in parts orderby p.Count select p).ToArray();
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
					if (pair.Value.RoomId == room.Id)
					{
						continue;
					}

					var targetRoom = GetRoomById(pair.Value.RoomId);
					if (targetRoom.Position == null)
					{
						continue;
					}

					if (targetRoom.Connections.Count != 1)
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

		private static int CalculateMovedRoomKey(Point pos) => pos.Y * 10000 + pos.X;

		public List<List<Tuple<MMBRoom, Point>>> ToRoomsByPos(Dictionary<int, Point> movedRooms)
		{
			var movedRoomsByPos = new Dictionary<int, List<Tuple<MMBRoom, Point>>>();
			foreach (var pair in movedRooms)
			{
				var room = GetRoomById(pair.Key);
				if (room.Position == null)
				{
					continue;
				}

				var pos = room.Position.Value;
				pos.X += pair.Value.X;
				pos.Y += pair.Value.Y;
				var key = CalculateMovedRoomKey(pos);

				List<Tuple<MMBRoom, Point>> rooms;
				if (!movedRoomsByPos.TryGetValue(key, out rooms))
				{
					rooms = new List<Tuple<MMBRoom, Point>>();
					movedRoomsByPos[key] = rooms;
				}

				rooms.Add(new Tuple<MMBRoom, Point>(room, pos));
			}

			return movedRoomsByPos.Values.ToList();
		}

		public MeasurePushRoomResult MeasurePushRoom(int firstRoomId, Point forceVector)
		{
			// Determine rooms movement
			var movedRooms = new Dictionary<int, Point>();

			// Horizontal movement
			for (var x = 0; x < Math.Abs(forceVector.X); ++x)
			{
				var toProcess = new IdQueue(firstRoomId);
				while (toProcess.Count > 0)
				{
					var id = toProcess.Pop();
					Point sourceForce;
					if (!movedRooms.TryGetValue(id, out sourceForce))
					{
						sourceForce = new Point();
					}

					var delta = Math.Sign(forceVector.X);

					var room = GetRoomById(id);
					var pos = room.Position.Value;
					pos.X += sourceForce.X;
					pos.Y += sourceForce.Y;

					// Process neighbour rooms
					foreach (var pair in room.Connections)
					{
						var exitDir = pair.Key;

						var targetRoom = GetRoomById(pair.Value.RoomId);
						if (targetRoom.Position == null || toProcess.Contains(targetRoom.Id))
						{
							continue;
						}

						// Skip broken connections
						var targetPos = targetRoom.Position.Value;
						Point targetForce;
						if (movedRooms.TryGetValue(targetRoom.Id, out targetForce))
						{
							targetPos.X += targetForce.X;
							targetPos.Y += targetForce.Y;
						}

						if (!IsConnectionStraight(pos, targetPos, exitDir))
						{
							continue;
						}

						// Skip horizontal connection to the opposite direction
						// Or horizontal connection to the same direction longer than 1
						var dist = Math.Abs(targetPos.X - pos.X);
						if (delta == 1)
						{
							if (exitDir == MMBDirection.West || exitDir == MMBDirection.Down)
							{
								continue;
							}

							if ((exitDir == MMBDirection.East || exitDir == MMBDirection.Up) && dist > 1)
							{
								continue;
							}
						}
						else
						{
							if (exitDir == MMBDirection.East || exitDir == MMBDirection.Up)
							{
								continue;
							}

							if ((exitDir == MMBDirection.West || exitDir == MMBDirection.Down) && dist > 1)
							{
								continue;
							}
						}

						toProcess.Add(targetRoom.Id);
					}

					// Finally move the room
					sourceForce.X += delta;
					movedRooms[id] = sourceForce;
				}
			}

			// Vertical movement
			for (var y = 0; y < Math.Abs(forceVector.Y); ++y)
			{
				var toProcess = new IdQueue(firstRoomId);
				while (toProcess.Count > 0)
				{
					var id = toProcess.Pop();
					Point sourceForce;
					if (!movedRooms.TryGetValue(id, out sourceForce))
					{
						sourceForce = new Point();
					}

					var delta = Math.Sign(forceVector.Y);

					var room = GetRoomById(id);
					var pos = room.Position.Value;
					pos.X += sourceForce.X;
					pos.Y += sourceForce.Y;

					// Process neighbour rooms
					foreach (var pair in room.Connections)
					{
						var exitDir = pair.Key;

						var targetRoom = GetRoomById(pair.Value.RoomId);
						if (targetRoom.Position == null || toProcess.Contains(targetRoom.Id))
						{
							continue;
						}

						// Skip broken connections
						var targetPos = targetRoom.Position.Value;
						Point targetForce;
						if (movedRooms.TryGetValue(targetRoom.Id, out targetForce))
						{
							targetPos.X += targetForce.X;
							targetPos.Y += targetForce.Y;
						}

						if (!IsConnectionStraight(pos, targetPos, exitDir))
						{
							continue;
						}

						var dist = Math.Abs(targetPos.Y - pos.Y);
						if (delta == 1)
						{
							if (exitDir == MMBDirection.North || exitDir == MMBDirection.Up)
							{
								continue;
							}

							if ((exitDir == MMBDirection.South || exitDir == MMBDirection.Down) && dist > 1)
							{
								continue;
							}
						}
						else
						{
							if (exitDir == MMBDirection.South || exitDir == MMBDirection.Down)
							{
								continue;
							}

							if ((exitDir == MMBDirection.North || exitDir == MMBDirection.Up) && dist > 1)
							{
								continue;
							}
						}

						toProcess.Add(targetRoom.Id);
					}

					// Finally move the room
					sourceForce.Y += delta;
					movedRooms[id] = sourceForce;
				}
			}

			// Remove overlapped rooms
			var roomsByPos = ToRoomsByPos(movedRooms);
			foreach (var rooms in roomsByPos)
			{
				if (rooms.Count < 2)
				{
					continue;
				}

				var pos = rooms[0].Item2;
				for (var i = 1; i < rooms.Count; ++i)
				{
					var room = rooms[i];
					movedRooms.Remove(room.Item1.Id);
				}
			}

			// Now build deletion list of non-moved rooms
			var deletedRooms = new HashSet<int>();
			foreach (var pair in movedRooms)
			{
				var room = GetRoomById(pair.Key);
				var delta = pair.Value;

				var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);
				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !movedRooms.ContainsKey(existingRoom.Id))
				{
					deletedRooms.Add(existingRoom.Id);
				}
			}

			return new MeasurePushRoomResult(
				(from pair in movedRooms select new MeasurePushRoomMovement(GetRoomById(pair.Key), pair.Value)).ToArray(),
				(from d in deletedRooms select GetRoomById(d)).ToArray());
		}

		private void MeasureCompactPushAddConnectionNeighbours(MMBDirection pushDirection, MMBRoom sourceRoom, MMBRoom targetRoom, MMBDirection exitDir, IdQueue processor)
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
					if (neighbourRoom != null && !processor.WasProcessed(neighbourRoom.Id))
					{
						processor.Add(neighbourRoom.Id);
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
					if (neighbourRoom != null && !processor.WasProcessed(neighbourRoom.Id))
					{
						processor.Add(neighbourRoom.Id);
					}
				}
			}
		}

		public MeasurePushRoomResult MeasureCompactPushRoom(int firstRoomId, MMBDirection direction)
		{
			// Determine rooms movement
			var delta = direction.GetDelta();
			var toProcess = new IdQueue(firstRoomId);

			while (toProcess.Count > 0)
			{
				var id = toProcess.Pop();
				var room = GetRoomById(id);

				// Process connected rooms
				var sourcePos = room.Position.Value;
				foreach (var pair in room.Connections)
				{
					var exitDir = pair.Key;
					var targetRoom = GetRoomById(pair.Value.RoomId);
					if (targetRoom.Position == null || toProcess.WasProcessed(pair.Value.RoomId))
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
						toProcess.Add(targetRoom.Id);
					}

					// Add rooms neighbor to connection
					MeasureCompactPushAddConnectionNeighbours(direction, room, targetRoom, exitDir, toProcess);
				}

				// Add neighbor room
				var neighbourPos = new Point(sourcePos.X + delta.X, sourcePos.Y + delta.Y);
				var neighbourRoom = GetRoomByPosition(neighbourPos);
				if (neighbourRoom != null && !toProcess.WasProcessed(neighbourRoom.Id))
				{
					toProcess.Add(neighbourRoom.Id);
				}
			}

			// Determine deleted rooms
			var movedRoomsList = new List<MeasurePushRoomMovement>();
			var deletedRooms = new List<MMBRoom>();
			foreach (var pair in toProcess.Processed)
			{
				var room = GetRoomById(pair);

				movedRoomsList.Add(new MeasurePushRoomMovement(room, delta));

				var newPos = new Point(room.Position.Value.X + delta.X, room.Position.Value.Y + delta.Y);

				var existingRoom = GetRoomByPosition(newPos);
				if (existingRoom != null && !toProcess.WasProcessed(existingRoom.Id))
				{
					deletedRooms.Add(existingRoom);
				}
			}

			return new MeasurePushRoomResult(movedRoomsList.ToArray(), deletedRooms.ToArray());
		}

		public bool IsReachable(int firstRoomId, int secondRoomId)
		{
			var toProcess = new IdQueue(firstRoomId);

			while (toProcess.Count > 0)
			{
				var id = toProcess.Pop();
				var room = GetRoomById(id);

				foreach (var pair in room.Connections)
				{
					var targetId = pair.Value.RoomId;
					if (targetId == secondRoomId)
					{
						return true;
					}

					var targetRoom = GetRoomById(targetId);
					if (toProcess.WasProcessed(targetId))
					{
						continue;
					}

					toProcess.Add(targetId);
				}
			}

			return false;
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

		public MMBArea Clone()
		{
			var result = new MMBArea
			{
				Name = Name,
				BackgroundColor = BackgroundColor,
				LogMessage = LogMessage,
				Tag = Tag
			};

			foreach (var r in _roomsByIds)
			{
				result.Add(r.Value.Clone());
			}

			return result;
		}

		public override string ToString() => Name;

		public static bool AreEqual(MMBArea a, MMBArea b)
		{
			a = a.Clone();
			a.DeleteEmptyColsRows();

			b = b.Clone();
			b.DeleteEmptyColsRows();

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