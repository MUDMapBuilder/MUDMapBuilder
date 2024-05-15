using SkiaSharp;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;

namespace MUDMapBuilder
{
	partial class PositionedRooms
	{
		private static readonly SKColor DefaultColor = SKColors.Black;
		private static readonly SKColor ConnectionWithObstacles = SKColors.Red;
		private static readonly SKColor NonStraightConnection = SKColors.Yellow;
		private static readonly SKColor LongConnection = SKColors.Green;

		private const int RoomHeight = 32;
		private const int TextPadding = 8;
		private static readonly Point RoomSpace = new Point(32, 32);
		private int[] _cellsWidths;

		private bool AreRoomsConnected(Point a, Point b, MMBDirection direction)
		{
			var room = GetRoomByZeroBasedPosition(a);
			if (room.HasDrawnConnection(direction, b))
			{
				return true;
			}

			room = GetRoomByZeroBasedPosition(b);
			if (room.HasDrawnConnection(direction.GetOppositeDirection(), a))
			{
				return true;
			}

			return false;
		}

		public MMBImageResult BuildPng()
		{
			var roomInfos = new List<MMBImageRoomInfo>();

			byte[] imageBytes = null;

			var mapRect = CalculateRectangle();
			var width = mapRect.Width;
			var height = mapRect.Height;
			using (SKPaint paint = new SKPaint())
			{
				paint.Color = SKColors.Black;
				paint.IsAntialias = true;
				paint.Style = SKPaintStyle.Stroke;
				paint.TextAlign = SKTextAlign.Center;

				// First grid run - determine cells width
				_cellsWidths = new int[width];
				for (var x = 0; x < width; ++x)
				{
					for (var y = 0; y < height; ++y)
					{
						var room = GetRoomByZeroBasedPosition(x, y);
						if (room == null)
						{
							continue;
						}

						room.ClearDrawnConnections();

						var sz = (int)(paint.MeasureText(room.Room.ToString()) + TextPadding * 2 + 0.5f);
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

				imageWidth += (width + 1) * RoomSpace.X;

				SKImageInfo imageInfo = new SKImageInfo(imageWidth,
														height * RoomHeight + (height + 1) * RoomSpace.Y);


				using (SKSurface surface = SKSurface.Create(imageInfo))
				{
					SKCanvas canvas = surface.Canvas;

					for (var x = 0; x < width; ++x)
					{
						for (var y = 0; y < height; ++y)
						{
							var room = GetRoomByZeroBasedPosition(x, y);
							if (room == null)
							{
								continue;
							}

							// Draw room
							var rect = GetRoomRect(new Point(x, y));
							paint.StrokeWidth = 2;

							if (room.Id == SelectedRoomId)
							{
								paint.Color = SKColors.Green;
							}
							else if (room.MarkColor != null)
							{
								paint.Color = room.MarkColor.Value;
							}
							else
							{
								paint.Color = DefaultColor;
							}

							canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, paint);
							roomInfos.Add(new MMBImageRoomInfo(room.Room, rect));

							// Draw connections
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

								var targetPos = ToZeroBasedPosition(targetRoom.Position.Value);
								if (AreRoomsConnected(new Point(x, y), targetPos, exitDir))
								{
									// Connection is drawn already
									continue;
								}

								var oppositeDir = exitDir.GetOppositeDirection();
								var isTwoWay = targetRoom.Room.Exits.ContainsKey(oppositeDir) &&
									targetRoom.Room.Exits[oppositeDir].Id == room.Id;

								if (isTwoWay)
								{
									paint.Color = DefaultColor;
								}
								else
								{
									paint.Color = SKColors.Magenta;
								}

								var isStraight = true;
								switch (exitDir)
								{
									case MMBDirection.North:
										if (x == targetPos.X && y < targetPos.Y)
										{
											isStraight = false;
										}
										break;
									case MMBDirection.East:
										if (x > targetPos.X && y == targetPos.Y)
										{
											isStraight = false;
										}
										break;
									case MMBDirection.South:
										if (x == targetPos.X && y > targetPos.Y)
										{
											isStraight = false;
										}
										break;
									case MMBDirection.West:
										if (x < targetPos.X && y == targetPos.Y)
										{
											isStraight = false;
										}
										break;
								}

								if (room.Id == targetRoom.Id)
								{
									isStraight = false;
								}

								if (BrokenConnections.WithObstacles.Find(room.Id, targetRoom.Id, exitDir) != null)
								{
									paint.Color = ConnectionWithObstacles;
								}
								else if (BrokenConnections.NonStraight.Find(room.Id, targetRoom.Id, exitDir) != null)
								{
									paint.Color = NonStraightConnection;
								}
								else if (BrokenConnections.Long.Find(room.Id, targetRoom.Id, exitDir) != null)
								{
									paint.Color = LongConnection;
								}
								else
								{
									paint.Color = DefaultColor;
								}

								if (isStraight)
								{
									// Straight connection
									// Source and target room are close to each other, hence draw the simple line
									var targetRect = GetRoomRect(targetPos);
									var sourceScreen = GetConnectionPoint(rect, exitDir);
									var targetScreen = GetConnectionPoint(targetRect, exitDir.GetOppositeDirection());
									canvas.DrawLine(sourceScreen.X, sourceScreen.Y, targetScreen.X, targetScreen.Y, paint);
								}
								else
								{
									// In other case we might have to use A* to draw the path
									// Basic idea is to consider every cell(spaces between rooms are cells too) as grid 2x2
									// Where 1 means center
									var steps = BuildPath(new Point(x, y), targetPos, exitDir);

									var src = steps[0];

									var points = new List<SKPoint>();
									for (var j = 1; j < steps.Count; j++)
									{
										var dest = steps[j];

										canvas.DrawLine(src.X, src.Y, dest.X, dest.Y, paint);

										src = dest;
									}
								}

								room.AddDrawnConnection(exitDir, targetPos);
							}

							paint.Color = DefaultColor;
							paint.StrokeWidth = 1;
							canvas.DrawText(room.Room.ToString(), rect.X + rect.Width / 2, rect.Y + rect.Height / 2, paint);

							if (room.ForceMark != null)
							{
								var sourceScreen = ToScreen(new Point(x, y));
								var tt = new Point(x + room.ForceMark.Value.X, y + room.ForceMark.Value.Y);
								var addX = 0;
								if (tt.X >= 0 && tt.X < _cellsWidths.Length)
								{
									addX = _cellsWidths[tt.X] / 2;
								}
								var targetScreen = ToScreen(tt);

								paint.Color = SKColors.DarkGreen;
								canvas.DrawLine(sourceScreen.X + rect.Width / 2,
									sourceScreen.Y + RoomHeight / 2,
									targetScreen.X + addX,
									targetScreen.Y + RoomHeight / 2, paint);
							}
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

			return new MMBImageResult(imageBytes, roomInfos.ToArray());
		}

		private List<Point> BuildPath(Point sourceGridPos, Point targetGridPos, MMBDirection direction)
		{
			var sourceRoomRect = GetRoomRect(sourceGridPos);

			// Add source connection point
			var sourcePos = GetConnectionPoint(sourceRoomRect, direction);
			var result = new List<Point>
			{
				sourcePos
			};

			var pathRadius = RoomSpace.X / 4;

			// Firstly add direction movement
			var delta = direction.GetDelta();
			sourcePos.X += delta.X * pathRadius;
			sourcePos.Y += delta.Y * pathRadius;
			result.Add(sourcePos);

			var targetRoomRect = GetRoomRect(targetGridPos);
			var targetConnectionPos = GetConnectionPoint(targetRoomRect, direction.GetOppositeDirection());
			var targetPos = targetConnectionPos;
			delta = direction.GetOppositeDirection().GetDelta();
			targetPos.X += delta.X * pathRadius;
			targetPos.Y += delta.Y * pathRadius;

			if (direction == MMBDirection.West || direction == MMBDirection.East)
			{
				if (sourceGridPos.Y == targetGridPos.Y)
				{
					// Go either up or down
					if (targetPos.Y < sourcePos.Y)
					{
						sourcePos = new Point(sourcePos.X, sourcePos.Y - pathRadius);
					}
					else
					{
						sourcePos = new Point(sourcePos.X, sourcePos.Y + pathRadius);
					}
					result.Add(sourcePos);
				}
				else
				{
					// Half of the vertical movement
					sourcePos = new Point(sourcePos.X, sourcePos.Y + (targetPos.Y - sourcePos.Y) / 2);
					result.Add(sourcePos);
				}

				// Horizontal movement
				sourcePos = new Point(targetPos.X, sourcePos.Y);
				result.Add(sourcePos);
			}
			else
			{
				if (sourceGridPos.X == targetGridPos.X)
				{
					// Go either left or right
					if (targetPos.X < sourcePos.X)
					{
						sourcePos = new Point(sourcePos.X - pathRadius, sourcePos.Y);
					}
					else
					{
						sourcePos = new Point(sourcePos.X + pathRadius, sourcePos.Y);
					}
					result.Add(sourcePos);
				}
				else
				{
					// Half of horizontal movement
					sourcePos = new Point(sourcePos.X + (targetPos.X - sourcePos.X) / 2, sourcePos.Y);
					result.Add(sourcePos);
				}

				// Vertical movement
				sourcePos = new Point(sourcePos.X, targetPos.Y);
				result.Add(sourcePos);
			}

			result.Add(targetPos);
			result.Add(targetConnectionPos);

			return result;
		}

		private Point ToScreen(Point pos)
		{
			if (pos.X >= _cellsWidths.Length)
			{
				pos.X = _cellsWidths.Length - 1;
			}

			var screenX = RoomSpace.X;
			for (var x = 0; x < pos.X; ++x)
			{
				screenX += _cellsWidths[x];
				screenX += RoomSpace.X;
			}

			return new Point(screenX, pos.Y * RoomHeight + (pos.Y + 1) * RoomSpace.Y);
		}

		private Rectangle GetRoomRect(Point pos)
		{
			var screen = ToScreen(pos);
			return new Rectangle(screen.X, screen.Y, _cellsWidths[pos.X], RoomHeight);
		}

		private static Point GetConnectionPoint(Rectangle rect, MMBDirection direction)
		{
			switch (direction)
			{
				case MMBDirection.North:
					return new Point(rect.X + rect.Width / 2, rect.Y);
				case MMBDirection.East:
					return new Point(rect.Right, rect.Y + rect.Height / 2);
				case MMBDirection.South:
					return new Point(rect.X + rect.Width / 2, rect.Bottom);
				case MMBDirection.West:
					return new Point(rect.Left, rect.Y + rect.Height / 2);
				case MMBDirection.Up:
					return new Point(rect.Right, rect.Y);
				case MMBDirection.Down:
					return new Point(rect.X, rect.Bottom);
			}

			throw new Exception($"Unknown direction {direction}");
		}
	}
}
