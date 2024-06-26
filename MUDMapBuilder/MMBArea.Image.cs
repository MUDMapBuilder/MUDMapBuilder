using SkiaSharp;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace MUDMapBuilder
{
	partial class MMBArea
	{
		private enum ConnectionType
		{
			Straight,
			NonStraight,
			Obstacled
		}

		private static readonly SKColor DefaultColor = SKColors.Black;
		private static readonly SKColor SelectedColor = SKColors.Green;
		private static readonly SKColor ExitToOtherAreaColor = SKColors.Blue;
		private static readonly SKColor ConnectionWithObstacles = SKColors.Red;
		private static readonly SKColor NonStraightConnection = SKColors.Yellow;
		private static readonly SKColor Intersection = SKColors.Magenta;
		private static readonly SKColor LongConnection = SKColors.Green;

		private const int RoomHeight = 32;
		private const int TextPadding = 8;
		private const int ArrowRadius = 8;
		private static readonly Point RoomSpace = new Point(32, 32);
		private int[] _cellsWidths;
		private bool[,] _nsConnections;

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

		public MMBImageResult BuildPng(BuildOptions options, bool colorizeConnectionIssues = true)
		{
			var roomInfos = new List<MMBImageRoomInfo>();

			byte[] imageBytes = null;

			var mapRect = CalculateRectangle();
			var width = mapRect.Width;
			var height = mapRect.Height;

			_nsConnections = new bool[width, height];
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

						var text = options.AddDebugInfo ? room.ToString() : room.Name;
						var sz = (int)(paint.MeasureText(text) + TextPadding * 2 + 0.5f);
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

					canvas.Clear(SKColors.White);
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
								paint.Color = SelectedColor;
							}
							else if (room.MarkColor != null)
							{
								paint.Color = room.MarkColor.Value;
							}
							else if (room.IsExitToOtherArea)
							{
								paint.Color = ExitToOtherAreaColor;
							}
							else
							{
								paint.Color = DefaultColor;
							}

							canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, paint);
							roomInfos.Add(new MMBImageRoomInfo(room, rect));

							// Draw connections
							foreach (var pair in room.Connections)
							{
								// Ignore backward connections
								if (pair.Value.ConnectionType == MMBConnectionType.Backward)
								{
									continue;
								}

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

								var targetPos = ToZeroBasedPosition(targetRoom.Position.Value);
								if (AreRoomsConnected(new Point(x, y), targetPos, exitDir))
								{
									// Connection is drawn already
									continue;
								}

								var ct = ConnectionType.Straight;
								switch (exitDir)
								{
									case MMBDirection.East:
										if (y != targetPos.Y)
										{
											break;
										}

										if (x > targetPos.X)
										{
											ct = ConnectionType.NonStraight;
										}
										else
										{
											for(var i = x + 1; i < targetPos.X; ++i)
											{
												if (GetRoomByZeroBasedPosition(new Point(i, y)) != null)
												{
													ct = ConnectionType.Obstacled;
													break;
												}
											}
										}
										break;

									case MMBDirection.West:
										if (y != targetPos.Y)
										{
											break;
										}

										if (x < targetPos.X)
										{
											ct = ConnectionType.NonStraight;
										}
										else
										{
											for (var i = x - 1; i > targetPos.X; --i)
											{
												if (GetRoomByZeroBasedPosition(new Point(i, y)) != null)
												{
													ct = ConnectionType.Obstacled;
													break;
												}
											}
										}
										break;

									case MMBDirection.North:
										if (x != targetPos.X)
										{
											break;
										}

										if (y < targetPos.Y)
										{
											ct = ConnectionType.NonStraight;
										}
										else
										{
											for (var i = y - 1; i > targetPos.Y; --i)
											{
												if (GetRoomByZeroBasedPosition(new Point(x, i)) != null)
												{
													ct = ConnectionType.Obstacled;
													break;
												}
											}
										}
										break;

									case MMBDirection.South:
										if (x != targetPos.X)
										{
											break;
										}

										if (y > targetPos.Y)
										{
											ct = ConnectionType.NonStraight;
										}
										else
										{
											for (var i = y + 1; i < targetPos.Y; ++i)
											{
												if (GetRoomByZeroBasedPosition(new Point(x, i)) != null)
												{
													ct = ConnectionType.Obstacled;
													break;
												}
											}
										}
										break;
								}

								if (room.Id == targetRoom.Id)
								{
									ct = ConnectionType.NonStraight;
								}

								if (colorizeConnectionIssues)
								{
									if (BrokenConnections.WithObstacles.Find(room.Id, targetRoom.Id, exitDir) != null)
									{
										paint.Color = ConnectionWithObstacles;
									}
									else if (BrokenConnections.NonStraight.Find(room.Id, targetRoom.Id, exitDir) != null)
									{
										paint.Color = NonStraightConnection;
									}
									else if (BrokenConnections.Intersections.Find(room.Id, targetRoom.Id, exitDir) != null)
									{
										paint.Color = Intersection;
									}
									else if (BrokenConnections.Long.Find(room.Id, targetRoom.Id, exitDir) != null)
									{
										paint.Color = LongConnection;
									}
									else
									{
										paint.Color = DefaultColor;
									}
								}
								else
								{
									paint.Color = DefaultColor;
								}

								var sourceScreen = GetConnectionPoint(rect, exitDir);
								var targetRect = GetRoomRect(targetPos);
								var targetScreen = GetConnectionPoint(targetRect, exitDir.GetOppositeDirection());
								if (ct == ConnectionType.Straight)
								{
									// Straight connection
									// Source and target room are close to each other, hence draw the simple line
									canvas.DrawLine(sourceScreen.X, sourceScreen.Y, targetScreen.X, targetScreen.Y, paint);
								}
								else
								{
									// In other case we might have to use A* to draw the path
									// Basic idea is to consider every cell(spaces between rooms are cells too) as grid 2x2
									// Where 1 means center
									List<Point> steps;

									if (ct == ConnectionType.NonStraight)
									{
										steps = BuildNsPath(new Point(x, y), targetPos, exitDir);
									} else
									{
										steps = BuildObstacledPath(new Point(x, y), targetPos, exitDir);
									}

									var src = steps[0];
									var points = new List<SKPoint>();
									for (var j = 1; j < steps.Count; j++)
									{
										var dest = steps[j];

										canvas.DrawLine(src.X, src.Y, dest.X, dest.Y, paint);

										src = dest;
									}

									sourceScreen = steps[0];
									targetScreen = steps[steps.Count - 1];
								}

								if (pair.Value.ConnectionType == MMBConnectionType.Forward)
								{
									// Draw single-way arrow
									var skPath = new SKPath();
									skPath.FillType = SKPathFillType.EvenOdd;
									skPath.MoveTo(0, 0);
									skPath.LineTo(-ArrowRadius, ArrowRadius);
									skPath.LineTo(-ArrowRadius, -ArrowRadius);
									skPath.LineTo(0, 0);
									skPath.Close();

									// Determine angle between two vectors
									var v1 = new Vector2(targetScreen.X - sourceScreen.X,
										targetScreen.Y - sourceScreen.Y);
									v1 = Vector2.Normalize(v1);
									var v2 = new Vector2(1, 0);
									var cosA = Vector2.Dot(v1, v2);
									var sinA = v1.X * v2.Y - v2.X * v1.Y;
									var angleInRads = (float)Math.Atan2(sinA, cosA);

									var tr = SKMatrix.Concat(SKMatrix.CreateTranslation(targetScreen.X, targetScreen.Y),
										SKMatrix.CreateRotation(-angleInRads));
									skPath.Transform(tr);

									var oldColor = paint.Color;
									paint.Style = SKPaintStyle.Fill;
									paint.Color = SKColors.White;
									canvas.DrawPath(skPath, paint);
									paint.Style = SKPaintStyle.Stroke;
									paint.Color = oldColor;
									canvas.DrawPath(skPath, paint);
								}

								room.AddDrawnConnection(exitDir, targetPos);
							}

							paint.Color = room.IsExitToOtherArea ? ExitToOtherAreaColor : DefaultColor;
							paint.StrokeWidth = 1;
							var text = options.AddDebugInfo ? room.ToString() : room.Name;
							canvas.DrawText(text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, paint);

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

		private void UpdateNsIntersects(Point p, ref bool intersects)
		{
			if (_nsConnections[p.X, p.Y])
			{
				intersects = true;
			}

			_nsConnections[p.X, p.Y] = true;
		}

		private List<Point> BuildNsPath(Point sourceGridPos, Point targetGridPos, MMBDirection direction)
		{
			var pathRadius = RoomSpace.X / 4;

			var sourceRoomRect = GetRoomRect(sourceGridPos);
			var sourcePos = GetConnectionPoint(sourceRoomRect, direction);

			var targetRoomRect = GetRoomRect(targetGridPos);
			var targetConnectionPos = GetConnectionPoint(targetRoomRect, direction.GetOppositeDirection());
			var targetPos = targetConnectionPos;
			var delta = direction.GetOppositeDirection().GetDelta();
			targetPos.X += delta.X * pathRadius;
			targetPos.Y += delta.Y * pathRadius;

			// Add source connection point
			var result = new List<Point>
			{
				sourcePos
			};

			// Firstly add direction movement
			delta = direction.GetDelta();
			sourcePos.X += delta.X * pathRadius;
			sourcePos.Y += delta.Y * pathRadius;
			result.Add(sourcePos);

			var intersects = false;

			// Set intersection flags and check if we intersect other ns connection
			var p = sourceGridPos;
			switch (direction)
			{
				case MMBDirection.East:
					for (; p.X <= targetGridPos.X; ++p.X)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.West:
					for (; p.X >= targetGridPos.X; --p.X)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.North:
					for (; p.Y >= targetGridPos.Y; --p.Y)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.South:
					for (; p.Y <= targetGridPos.Y; ++p.Y)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				default:
					throw new Exception($"Direction {direction} isn't supported");
			}

			if (direction == MMBDirection.West || direction == MMBDirection.East)
			{
				// Go either up or down
				if (intersects)
				{
					sourcePos = new Point(sourcePos.X, sourcePos.Y - pathRadius);
				}
				else
				{
					sourcePos = new Point(sourcePos.X, sourcePos.Y + pathRadius);
				}

				result.Add(sourcePos);

				sourcePos = new Point(targetPos.X, sourcePos.Y);
				result.Add(sourcePos);
			}
			else
			{
				// Go either left or right
				if (intersects)
				{
					sourcePos = new Point(sourcePos.X - pathRadius, sourcePos.Y);
				}
				else
				{
					sourcePos = new Point(sourcePos.X + pathRadius, sourcePos.Y);
				}

				result.Add(sourcePos);

				sourcePos = new Point(sourcePos.X, targetPos.Y);

				result.Add(sourcePos);
			}

			result.Add(targetPos);
			result.Add(targetConnectionPos);

			return result;
		}

		private List<Point> BuildObstacledPath(Point sourceGridPos, Point targetGridPos, MMBDirection direction)
		{
			var sourceRoomRect = GetRoomRect(sourceGridPos);
			var sourcePos = GetConnectionPoint(sourceRoomRect, direction);

			int pathRadius;
			if (direction == MMBDirection.East || direction == MMBDirection.West)
			{
				pathRadius = RoomSpace.X / 4;
			} else
			{
				pathRadius = sourceRoomRect.Width / 8;
			}


			var targetRoomRect = GetRoomRect(targetGridPos);
			var targetConnectionPos = GetConnectionPoint(targetRoomRect, direction.GetOppositeDirection());

			var result = new List<Point>();

			var intersects = false;

			// Set intersection flags and check if we intersect other ns connection
			var p = sourceGridPos;
			switch (direction)
			{
				case MMBDirection.East:
					for (; p.X <= targetGridPos.X; ++p.X)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.West:
					for (; p.X >= targetGridPos.X; --p.X)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.North:
					for (; p.Y >= targetGridPos.Y; --p.Y)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				case MMBDirection.South:
					for (; p.Y <= targetGridPos.Y; ++p.Y)
					{
						UpdateNsIntersects(p, ref intersects);
					}
					break;

				default:
					throw new Exception($"Direction {direction} isn't supported");
			}

			if (direction == MMBDirection.West || direction == MMBDirection.East)
			{
				// Go either up or down
				if (intersects)
				{
					sourcePos = new Point(sourcePos.X, sourcePos.Y - pathRadius);
				}
				else
				{
					sourcePos = new Point(sourcePos.X, sourcePos.Y + pathRadius);
				}

				result.Add(sourcePos);

				sourcePos = new Point(targetConnectionPos.X, sourcePos.Y);

				result.Add(sourcePos);
			}
			else
			{
				// Go either left or right
				if (intersects)
				{
					sourcePos = new Point(sourcePos.X - pathRadius, sourcePos.Y);
				}
				else
				{
					sourcePos = new Point(sourcePos.X + pathRadius, sourcePos.Y);
				}

				result.Add(sourcePos);

				// Vertical movement
				sourcePos = new Point(sourcePos.X, targetConnectionPos.Y);

				result.Add(sourcePos);
			}

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