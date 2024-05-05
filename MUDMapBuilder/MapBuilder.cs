using AbarimMUD.Data;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	public class MapBuilder
	{
		private Area _area;
		private readonly List<MMBRoom> _rooms = new List<MMBRoom>();

		private void PushRoom(Point pos, Direction dir)
		{
			foreach (var room in _rooms)
			{
				var roomPos = room.Position;
				switch (dir)
				{
					case Direction.North:
						if (roomPos.Y <= pos.Y)
						{
							--roomPos.Y;
						}
						break;
					case Direction.East:
						if (roomPos.X >= pos.X)
						{
							++roomPos.X;
						}
						break;
					case Direction.South:
						if (roomPos.Y >= pos.Y)
						{
							++roomPos.Y;
						}
						break;
					case Direction.West:
						if (roomPos.X <= pos.X)
						{
							--roomPos.X;
						}
						break;
					case Direction.Up:
						if (roomPos.Y <= pos.Y || roomPos.X >= pos.X)
						{
							--roomPos.Y;
							++roomPos.X;
						}
						break;
					case Direction.Down:
						if (roomPos.Y >= pos.Y || roomPos.X <= pos.X)
						{
							++roomPos.Y;
							--roomPos.X;
						}
						break;
				}

				room.Position = roomPos;
			}
		}

		private MMBRoom GetRoom(Room room) => (from r in _rooms where r.Room == room select r).FirstOrDefault();
		private MMBRoom GetRoomByPosition(Point pos) => (from r in _rooms where r.Position == pos select r).FirstOrDefault();

		public MMBGrid BuildGrid(Area area, int? maxSteps = null)
		{
			_area = area;

			var toProcess = new List<MMBRoom>();
			var firstRoom = new MMBRoom(area.Rooms[0])
			{
				Position = new Point(0, 0)
			};
			toProcess.Add(firstRoom);
			_rooms.Add(firstRoom);

			// First run: we move through room's exits, assigning each room a 2d coordinate
			// If there are overlaps, then we expand the grid in the direction of movement
			var step = 1;

			Point pos;
			while (toProcess.Count > 0 && (maxSteps == null || maxSteps.Value > step))
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);

				foreach (var exit in room.Room.Exits)
				{
					if (exit.TargetRoom == null || exit.TargetRoom.AreaId != _area.Id || GetRoom(exit.TargetRoom) != null)
					{
						continue;
					}

					pos = room.Position;
					var delta = exit.Direction.GetDelta();
					var newPos = new Point(pos.X + delta.X, pos.Y + delta.Y);

					while (true)
					{
						// Check if this pos is used already
						if (GetRoomByPosition(newPos) == null)
						{
							break;
						}

						PushRoom(newPos, exit.Direction);
					}

					var mbRoom = new MMBRoom(exit.TargetRoom)
					{
						Position = newPos
					};
					toProcess.Add(mbRoom);
					_rooms.Add(mbRoom);
				}

				++step;
			}

			// Second run: if it is possible to place interconnected rooms with single exits next to each other, do it
			/*			foreach (var room in _rooms)
						{
							pos = room.Position;
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
						}*/

			// Determine minimum point
			var min = new Point();
			var minSet = false;
			foreach (var room in _rooms)
			{
				pos = room.Position;
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
			foreach (var room in _rooms)
			{
				pos = room.Position;

				pos.X -= min.X;
				pos.Y -= min.Y;

				room.Position = pos;
			}

			// Determine size
			Point max = new Point(0, 0);
			foreach (var room in _rooms)
			{
				pos = room.Position;

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
					var room = GetRoomByPosition(new Point(x, y));
					if (room == null)
					{
						continue;
					}

					grid[x, y] = room;
				}
			}

			return grid;
		}
	}
}