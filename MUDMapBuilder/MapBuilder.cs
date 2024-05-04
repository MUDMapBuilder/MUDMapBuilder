using AbarimMUD.Data;
using GoRogue.MapViews;
using GoRogue;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Direction = AbarimMUD.Data.Direction;
using Rectangle = System.Drawing.Rectangle;
using GoRogue.Pathing;
using System.Numerics;

namespace MUDMapBuilder
{
	public class MapBuilder
	{
		private const int RoomHeight = 32;
		private const int TextPadding = 8;
		private static readonly Point RoomSpace = new Point(32, 32);

		private Area _area;
		private MMBGrid _grid;
		private int[] _cellsWidths;

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

			_grid = new MMBGrid(max);
			for (var x = 0; x < max.X; ++x)
			{
				for (var y = 0; y < max.Y; ++y)
				{
					var room = GetRoomByPoint(new Point(x, y));
					if (room == null)
					{
						continue;
					}

					_grid[x, y] = new MMBRoom(room);
				}
			}

			return _grid;
		}

		public byte[] BuildPng(Area area, int? maxSteps = null)
		{
			BuildGrid(area, maxSteps);

			byte[] imageBytes = null;
			using (SKPaint paint = new SKPaint())
			{
				paint.Color = SKColors.Black;
				paint.IsAntialias = true;
				paint.Style = SKPaintStyle.Stroke;
				paint.TextAlign = SKTextAlign.Center;

				// First grid run - determine cells width
				_cellsWidths = new int[_grid.Width];
				for (var x = 0; x < _grid.Width; ++x)
				{
					for (var y = 0; y < _grid.Height; ++y)
					{
						var room = _grid[x, y];
						if (room == null)
						{
							continue;
						}

						var sz = (int)(paint.MeasureText(room.Room.Name) + TextPadding * 2 + 0.5f);
						if (sz > _cellsWidths[x])
						{
							_cellsWidths[x] = sz;
						}
					}
				}


				// Second run - draw the map
				var imageWidth = 0;
				for (var i = 0; i < _cellsWidths.Length; ++i)
				{
					imageWidth += _cellsWidths[i];
				}

				imageWidth += (_grid.Width + 1) * RoomSpace.X;

				SKImageInfo imageInfo = new SKImageInfo(imageWidth,
														_grid.Height * RoomHeight + (_grid.Height + 1) * RoomSpace.Y);


				using (SKSurface surface = SKSurface.Create(imageInfo))
				{
					SKCanvas canvas = surface.Canvas;

					for (var x = 0; x < _grid.Width; ++x)
					{
						for (var y = 0; y < _grid.Height; ++y)
						{
							var mMBRoom = _grid[x, y];
							if (mMBRoom == null)
							{
								continue;
							}

							// Draw room
							var rect = GetRoomRect(new Point(x, y));
							paint.StrokeWidth = 2;
							canvas.DrawRect(rect.X, rect.Y, _cellsWidths[x], RoomHeight, paint);

							// Draw connections
							foreach (var roomExit in mMBRoom.Room.Exits)
							{
								if (roomExit.TargetRoom == null || roomExit.TargetRoom.Tag == null)
								{
									continue;
								}

								var targetPos = (Point)roomExit.TargetRoom.Tag;
								if (_grid.AreRoomsConnected(new Point(x, y), targetPos, roomExit.Direction))
								{
									// Connection is drawn already
									continue;
								}

								// Check if the straight connection could be drawn
								var straightConnection = false;

								Point? startCheck = null;
								Point? endCheck = null;

								switch (roomExit.Direction)
								{
									case Direction.North:
										if (y - targetPos.Y == 1)
										{
											straightConnection = true;
										}
										else if (y - targetPos.Y > 1)
										{
											startCheck = new Point(x, y - 1);
											endCheck = new Point(targetPos.X, targetPos.Y + 1);
										}
										break;
									case Direction.East:
										if (targetPos.X - x == 1)
										{
											straightConnection = true;
										}
										else if (targetPos.X - x > 1)
										{
											startCheck = new Point(x + 1, y);
											endCheck = new Point(targetPos.X - 1, targetPos.Y);
										}
										break;
									case Direction.South:
										if (targetPos.Y - y == 1)
										{
											straightConnection = true;
										}
										else if (targetPos.Y - 1 > 1)
										{
											startCheck = new Point(x, y + 1);
											endCheck = new Point(targetPos.X, targetPos.Y - 1);
										}
										break;
									case Direction.West:
										if (x - targetPos.X == 1)
										{
											straightConnection = true;
										}
										else if (x - targetPos.X > 1)
										{
											startCheck = new Point(x + 1, y);
											endCheck = new Point(targetPos.X - 1, targetPos.Y);
										}
										break;
									case Direction.Up:
										if (targetPos.X - x == 1)
										{
											straightConnection = true;
										}
										else if (y - targetPos.Y >= 1 && targetPos.X - x >= 1)
										{
											startCheck = new Point(x + 1, y - 1);
											endCheck = new Point(targetPos.X - 1, targetPos.Y + 1);
										}
										break;
									case Direction.Down:
										if (x - targetPos.X == 1)
										{
											straightConnection = true;
										}
										else if (targetPos.Y - y >= 1 && x - targetPos.X >= 1)
										{
											startCheck = new Point(x - 1, y + 1);
											endCheck = new Point(targetPos.X + 1, targetPos.Y - 1);
										}
										break;
								}

								if (startCheck != null && endCheck != null)
								{
									straightConnection = true;
									for (var checkX = Math.Min(startCheck.Value.X, endCheck.Value.X); checkX <= Math.Max(startCheck.Value.X, endCheck.Value.X); ++checkX)
									{
										for (var checkY = Math.Min(startCheck.Value.Y, endCheck.Value.Y); checkY <= Math.Max(startCheck.Value.Y, endCheck.Value.Y); ++checkY)
										{
											if (_grid[checkX, checkY] != null)
											{
												straightConnection = false;
												goto finishCheck;
											}
										}
									}

								finishCheck:;
								}

								if (straightConnection)
								{
									// Source and target room are close to each other, hence draw the simple line
									var targetRect = GetRoomRect(targetPos);
									var sourceScreen = GetConnectionPoint(rect, roomExit.Direction);
									var targetScreen = GetConnectionPoint(targetRect, roomExit.Direction.GetOppositeDirection());
									canvas.DrawLine(sourceScreen.X, sourceScreen.Y, targetScreen.X, targetScreen.Y, paint);
								}
								else
								{
									// In other case we might have to use A* to draw the path
									// Basic idea is to consider every cell(spaces between rooms are cells too) as grid 2x2
									// Where 1 means center
									var aStarSourceCoords = new Point(x, y).ToAStarCoord() + roomExit.Direction.ToAStarCoord();
									var aStarTargetCoords = targetPos.ToAStarCoord() + roomExit.Direction.GetOppositeDirection().ToAStarCoord();

									var aStarView = new AStarView(_grid);
									var pathFinder = new AStar(aStarView, Distance.MANHATTAN);
									var path = pathFinder.ShortestPath(aStarSourceCoords, aStarTargetCoords);
									var steps = path.Steps.ToArray();

									var src = aStarSourceCoords;
									for (var i = 0; i < steps.Length; i++)
									{
										var dest = steps[i];

										var sourceScreen = ToScreenCoord(src);
										var targetScreen = ToScreenCoord(dest);
										canvas.DrawLine(sourceScreen.X, sourceScreen.Y, targetScreen.X, targetScreen.Y, paint);

										src = dest;
									}
								}

								mMBRoom.Connect(roomExit.Direction, targetPos);
							}

							paint.StrokeWidth = 1;
							canvas.DrawText(mMBRoom.Room.Name, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, paint);
						}
					}

					using (SKImage image = surface.Snapshot())
					using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
					using (MemoryStream mStream = new MemoryStream(data.ToArray()))
					{
						imageBytes = data.ToArray();
					}
				}

			}

			return imageBytes;
		}

		private Rectangle GetRoomRect(Point pos)
		{
			var screenX = RoomSpace.X;
			for (var x = 0; x < pos.X; ++x)
			{
				screenX += _cellsWidths[x];
				screenX += RoomSpace.X;
			}

			return new Rectangle(screenX, pos.Y * RoomHeight + (pos.Y + 1) * RoomSpace.Y, _cellsWidths[pos.X], RoomHeight);
		}

		private Point ToScreenCoord(Coord coord)
		{
			// Shift by initial space
			coord -= new Coord(2, 2);

			// Determine grid coord
			var gridCoord = new Point(coord.X / 4, coord.Y / 4);

			// Calculate cell screen coords
			var screenX = RoomSpace.X;
			for (var x = 0; x < gridCoord.X; ++x)
			{
				screenX += _cellsWidths[x];
				screenX += RoomSpace.X;
			}

			var screenY = gridCoord.Y * RoomHeight + (gridCoord.Y + 1) * RoomSpace.Y;

			switch (coord.X % 4)
			{
				case 1:
					screenX += _cellsWidths[gridCoord.X] / 2;
					break;
				case 2:
					screenX += _cellsWidths[gridCoord.X];
					break;
				case 3:
					screenX += _cellsWidths[gridCoord.X] + RoomSpace.X / 2;
					break;
			}

			switch (coord.Y % 4)
			{
				case 1:
					screenY += RoomHeight / 2;
					break;
				case 2:
					screenY += RoomHeight;
					break;
				case 3:
					screenY += RoomHeight + RoomSpace.Y / 2;
					break;
			}

			return new Point(screenX, screenY);
		}

		private static Point GetConnectionPoint(Rectangle rect, Direction direction)
		{
			switch (direction)
			{
				case Direction.North:
					return new Point(rect.X + rect.Width / 2, rect.Y);
				case Direction.East:
					return new Point(rect.Right, rect.Y + rect.Height / 2);
				case Direction.South:
					return new Point(rect.X + rect.Width / 2, rect.Bottom);
				case Direction.West:
					return new Point(rect.Left, rect.Y + rect.Height / 2);
				case Direction.Up:
					return new Point(rect.Right, rect.Y);
				case Direction.Down:
					return new Point(rect.X, rect.Bottom);
			}

			throw new Exception($"Unknown direction {direction}");
		}

		private class AStarView : IMapView<bool>
		{
			private readonly MMBGrid _grid;

			public bool this[Coord pos] => CheckMove(pos.ToVector2());

			public bool this[int index1D] => throw new NotImplementedException();

			public bool this[int x, int y] => CheckMove(new Vector2(x, y));

			public int Height => _grid.Height * 4 + 2;

			public int Width => _grid.Width * 4 + 2;

			public AStarView(MMBGrid grid)
			{
				_grid = grid;
			}

			public bool CheckMove(Vector2 coord)
			{
				// Firstly determine whether wether we're at cell or space zone
				if (coord.X < 2 || coord.Y < 2)
				{
					// Space
					return true;
				}

				// Shift by initial space
				coord.X -= 2;
				coord.Y -= 2;

				var cx = coord.X % 4;
				var cy = coord.Y % 4;
				if (cx > 2 || cy > 2)
				{
					// Space
					return true;
				}

				// Cell
				var gridPoint = new Point((int)(coord.X / 4), (int)(coord.Y / 4));
				var room = _grid[gridPoint];

				return room == null;
			}
		}
	}

	internal static class MapBuilderExtensions
	{
		public static Point GetDelta(this Direction direction)
		{
			switch (direction)
			{
				case Direction.East:
					return new Point(1, 0);
				case Direction.West:
					return new Point(-1, 0);
				case Direction.North:
					return new Point(0, -1);
				case Direction.South:
					return new Point(0, 1);
				case Direction.Up:
					return new Point(1, -1);
				case Direction.Down:
					return new Point(-1, 1);
			}

			throw new Exception($"Unknown direction {direction}");
		}

		public static Coord ToAStarCoord(this Direction direction)
		{
			switch (direction)
			{
				case Direction.North:
					return new Coord(1, 0);
				case Direction.East:
					return new Coord(2, 1);
				case Direction.South:
					return new Coord(1, 2);
				case Direction.West:
					return new Coord(0, 1);
				case Direction.Up:
					return new Coord(2, 0);
				case Direction.Down:
					return new Coord(0, 2);
			}

			throw new Exception($"Unknown direction {direction}");
		}

		public static Coord ToAStarCoord(this Point source) => new Coord(source.X * 4 + 2, source.Y * 4 + 2);
		public static Point ToGridPoint(this Coord source) => new Point((source.X - 2) / 4, (source.Y - 2) / 4);
		public static Vector2 ToVector2(this Coord source) => new Vector2(source.X, source.Y);
		public static Coord ToCoord(this Vector2 source) => new Coord((int)source.X, (int)source.Y);
	}
}