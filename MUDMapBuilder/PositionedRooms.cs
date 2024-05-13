using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

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

		private readonly List<MMBRoom> _rooms = new List<MMBRoom>();
		private readonly Dictionary<int, MMBRoom> _roomsByIds = new Dictionary<int, MMBRoom>();
		private Rectangle _roomsRectangle;
		private MMBRoom[,] _roomsByPositions = null;
		private BrokenConnectionsInfo _brokenConnections;

		public int Count => _rooms.Count;

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

		internal MMBRoom this[int index] => _rooms[index];

		public int? SelectedRoomId { get; set; }

		private void OnRoomInvalid(object sender, EventArgs e) => InvalidatePositions();

		internal void Add(MMBRoom room)
		{
			room.Invalid += OnRoomInvalid;

			_roomsByIds[room.Id] = room;
			_rooms.Add(room);
			InvalidatePositions();
		}

		internal void Remove(int roomId)
		{
			var room = GetRoomById(roomId);
			room.Invalid -= OnRoomInvalid;
			_roomsByIds.Remove(roomId);
			_rooms.Remove(room);

			InvalidatePositions();
		}

		public void InvalidatePositions()
		{
			_roomsByPositions = null;
			_brokenConnections = null;
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
			obstacles = new HashSet<int>();
			var sourcePos = sourceRoom.Position;
			var targetPos = targetRoom.Position;

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

						/*					var asConnectionsCell = cell as MMBConnectionsCell;
											if (asConnectionsCell != null && asConnectionsCell.Count > 1)
											{
												noObstacles = false;
												break;
											}*/
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

			foreach (var room in _rooms)
			{
				var pos = room.Position;
				foreach (var pair in room.Room.Exits)
				{
					var exitDir = pair.Key;
					var exitRoom = pair.Value;

					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null)
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

			return result;
		}

		private Rectangle CalculateRectangle()
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

		private void UpdatePositions()
		{
			if (_roomsByPositions != null)
			{
				return;
			}

			_roomsRectangle = CalculateRectangle();

			_roomsByPositions = new MMBRoom[_roomsRectangle.Width, _roomsRectangle.Height];

			// Add rooms
			foreach (var room in _rooms)
			{
				var coord = new Point(room.Position.X - _roomsRectangle.X, room.Position.Y - _roomsRectangle.Y);
				_roomsByPositions[coord.X, coord.Y] = room;
			}

			_brokenConnections = CalculateBrokenConnections();
		}

		public void FixPlacementOfSingleExitRooms()
		{
			foreach (var room in _rooms)
			{
				var pos = room.Position;
				foreach (var pair in room.Room.Exits)
				{
					var exitDir = pair.Key;
					var exitRoom = pair.Value;

					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null)
					{
						continue;
					}

					if (exitRoom.Exits.Count > 1)
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
			foreach (var r in _rooms)
			{
				result.Add(r.Clone());
			}

			return result;
		}

		public IEnumerator<MMBRoom> GetEnumerator() => _rooms.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _rooms.GetEnumerator();

		public override int GetHashCode()
		{
			var result = 0;

			foreach (var room in _rooms)
			{
				result ^= room.Id;
				result ^= (room.Position.X << 8);
				result ^= (room.Position.Y << 16);
			}

			return result;
		}
	}
}