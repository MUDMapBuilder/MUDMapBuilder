using AbarimMUD.Data;
using GoRogue.MapViews;
using GoRogue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Direction = AbarimMUD.Data.Direction;
using Rectangle = System.Drawing.Rectangle;
using System.Numerics;

namespace MUDMapBuilder
{
	public class MapBuilder
	{
		private Area _area;

		private RoomExit GetExit(Room room, Direction dir)
		{
			var result = (from ex in room.Exits where ex.TargetRoom != null && ex.TargetRoom.AreaId == _area.Id && ex.Direction == dir select ex).FirstOrDefault();

			return result;
		}

		private Room GetRoomByPoint(Point p)
		{
			foreach (var room in _area.Rooms)
			{
				if (room.Tag == null)
				{
					continue;
				}

				var pos = (Point)room.Tag;

				if (pos == p)
				{
					return room;
				}
			}

			return null;
		}

		private void PushRoom(Room firstRoom, Direction dir)
		{
			// Push other rooms
			var pos = (Point)firstRoom.Tag;

			var toProcess = new List<Room>();
			var processed = new List<Room>();
			toProcess.Add(firstRoom);

			while (toProcess.Count > 0)
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);
				processed.Add(room);

				var roomPos = (Point)room.Tag;
				switch (dir)
				{
					case Direction.North:
						--roomPos.Y;
						break;
					case Direction.East:
						++roomPos.X;
						break;
					case Direction.South:
						++roomPos.Y;
						break;
					case Direction.West:
						--roomPos.X;
						break;
					case Direction.Up:
						--roomPos.Y;
						++roomPos.X;
						break;
					case Direction.Down:
						++roomPos.Y;
						--roomPos.X;
						break;
				}

				room.Tag = roomPos;

				foreach (var exit in room.Exits)
				{
					if (exit.TargetRoom == null || exit.TargetRoom.AreaId != _area.Id || exit.TargetRoom.Tag == null ||
						toProcess.Contains(exit.TargetRoom) || processed.Contains(exit.TargetRoom))
					{
						continue;
					}

					roomPos = (Point)exit.TargetRoom.Tag;

					var add = false;
					switch (dir)
					{
						case Direction.North:
							add = roomPos.Y <= pos.Y;
							break;
						case Direction.East:
							add = roomPos.X >= pos.X;
							break;
						case Direction.South:
							add = roomPos.Y >= pos.Y;
							break;
						case Direction.West:
							add = roomPos.X <= pos.X;
							break;
						case Direction.Up:
							add = roomPos.Y <= pos.Y || roomPos.X >= pos.X;
							break;
						case Direction.Down:
							add = roomPos.Y >= pos.Y || roomPos.X <= pos.X;
							break;
					}

					if (add)
					{
						toProcess.Add(exit.TargetRoom);
					}
				}
			}
		}

		public MMBGrid BuildGrid(Area area, int? maxSteps = null)
		{
			_area = area;

			// Reset rooms' tags
			foreach (var room in area.Rooms)
			{
				room.Tag = null;
			}

			var toProcess = new List<Room>();

			area.Rooms[0].Tag = new Point(0, 0);
			toProcess.Add(area.Rooms[0]);

			var step = 1;

			Point pos;
			while (toProcess.Count > 0 && (maxSteps == null || maxSteps.Value > step))
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);

				foreach (var exit in room.Exits)
				{
					if (exit.TargetRoom == null || exit.TargetRoom.AreaId != _area.Id || exit.TargetRoom.Tag != null)
					{
						continue;
					}

					pos = (Point)(room.Tag);
					var delta = exit.Direction.GetDelta();
					var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);

					while (true)
					{
						// Check if this pos is used already
						var intersectRoom = (from r in _area.Rooms where r != exit.TargetRoom && r.Tag != null && ((Point)r.Tag) == newPos select r).FirstOrDefault();
						if (intersectRoom == null)
						{
							break;
						}

						PushRoom(intersectRoom, exit.Direction);
					}

					exit.TargetRoom.Tag = newPos;
					toProcess.Add(exit.TargetRoom);
				}

				++step;
			}

			// Next run: if it is possible to place interconnected rooms with single exits next to each other, do it
			foreach (var room in _area.Rooms)
			{
				if (room.Tag == null)
				{
					continue;
				}

				pos = (Point)room.Tag;

				foreach (var exit in room.Exits)
				{
					if (exit.TargetRoom == null || exit.TargetRoom.AreaId != _area.Id || exit.TargetRoom.Tag == null)
					{
						continue;
					}

					var targetExitsCount = (from ex in exit.TargetRoom.Exits where ex.TargetRoom != null && ex.TargetRoom.AreaId == _area.Id select ex).Count();
					if (targetExitsCount > 1)
					{
						continue;
					}

					var targetPos = (Point)exit.TargetRoom.Tag;
					var delta = exit.Direction.GetDelta();
					var desiredPos = new Point(pos.X + delta.X, pos.Y + delta.Y);

					if (targetPos == desiredPos)
					{
						// Target room is already next to the source
						continue;
					}

					// Check if the spot is free
					var usedByRoom = GetRoomByPoint(desiredPos);
					if (usedByRoom != null)
					{
						// Spot is occupied
						continue;
					}

					// Place target room next to source
					exit.TargetRoom.Tag = desiredPos;
				}
			}

			// Determine minimum point
			var min = new Point();
			var minSet = false;
			foreach (var room in _area.Rooms)
			{
				if (room.Tag == null)
				{
					continue;
				}

				pos = (Point)room.Tag;
				if (!minSet)
				{
					min = new Point(pos.X, pos.Y);
					minSet = true;
				}

				if (pos.X < min.X)
				{
					min.X = pos.X;
				}

				if (pos.Y < min.Y)
				{
					min.Y = pos.Y;
				}
			}

			// Shift everything so it begins from 0,0
			Point shift = new Point(min.X < 0 ? -min.X : 0, min.Y < 0 ? -min.Y : 0);
			foreach (var room in _area.Rooms)
			{
				if (room.Tag == null)
				{
					continue;
				}

				pos = (Point)room.Tag;

				pos.X += shift.X;
				pos.Y += shift.Y;
				room.Tag = pos;
			}

			// Determine size
			Point max = new Point(0, 0);
			foreach (var room in _area.Rooms)
			{
				if (room.Tag == null)
				{
					continue;
				}

				pos = (Point)room.Tag;
				if (pos.X > max.X)
				{
					max.X = pos.X;
				}

				if (pos.Y > max.Y)
				{
					max.Y = pos.Y;
				}
			}

			++max.X;
			++max.Y;

			var grid = new MMBGrid(max, step);
			for (var x = 0; x < max.X; ++x)
			{
				for (var y = 0; y < max.Y; ++y)
				{
					var room = GetRoomByPoint(new Point(x, y));
					if (room == null)
					{
						continue;
					}

					grid[x, y] = new MMBRoom(room);
				}
			}

			return grid;
		}
	}
}