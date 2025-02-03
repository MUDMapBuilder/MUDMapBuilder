using SkiaSharp;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;

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

		private static readonly SKColor SelectedColor = SKColors.Green;
		private static readonly SKColor ConnectionWithObstacles = SKColors.Red;
		private static readonly SKColor NonStraightConnection = SKColors.Yellow;
		private static readonly SKColor Intersection = SKColors.Magenta;
		private static readonly SKColor LongConnection = SKColors.Green;

		private const int TextHorizontalPadding = 8;
		private const int TextVerticalPadding = 8;
		private const int ArrowRadius = 8;
		private const int DoorSize = 32;
		private static readonly Point RoomSpace = new Point(32, 32);
		private int[] _cellHeights;
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

				// First grid run - determine cells width and heights
				_cellsWidths = new int[width];
				_cellHeights = new int[height];
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

						// Width
						var text = options.AddDebugInfo ? room.ToString() : room.Name;

						var sz = paint.MeasureText(text) + TextHorizontalPadding * 2 + 0.5f;
						if (sz > _cellsWidths[x])
						{
							_cellsWidths[x] = (int)sz;
						}

						if (room.Contents != null && room.Contents.Count > 0)
						{
							for (var i = 0; i < room.Contents.Count; ++i)
							{
								sz = paint.MeasureText(room.Contents[i].Text) + TextHorizontalPadding * 2 + 0.5f;

								if (sz > _cellsWidths[x])
								{
									_cellsWidths[x] = (int)sz;
								}
							}
						}


						// Height
						sz = paint.FontSpacing;

						if (room.Contents != null && room.Contents.Count > 0)
						{
							sz += (room.Contents.Count + 1) * paint.FontSpacing;
						}

						sz += TextVerticalPadding * 2 + 0.5f;

						if (sz > _cellHeights[y])
						{
							_cellHeights[y] = (int)sz;
						}
					}
				}

				// Calculate total image size
				var imageWidth = 0;
				for (var i = 0; i < _cellsWidths.Length; ++i)
				{
					imageWidth += _cellsWidths[i];
				}

				imageWidth += (width + 1) * RoomSpace.X;

				var imageHeight = 0;
				for (var i = 0; i < _cellHeights.Length; ++i)
				{
					imageHeight += _cellHeights[i];
				}

				imageHeight += (height + 1) * RoomSpace.Y;

				// Second run - draw the map
				// But first, assign points of interest
				var pointOfInterestIdentifier = 1;
				var pointOfInterestRoomIdentifiers = new Dictionary<MMBRoom, int>();
				var pointOfInterestLines = new List<MMBRoomContentRecord>();

				for (var x = 0; x < width; ++x)
				{
					for (var y = 0; y < height; ++y)
					{
						var room = GetRoomByZeroBasedPosition(x, y);
						if (room == null)
						{
							continue;
						}

						if (!string.IsNullOrEmpty(room.PointOfInterestText))
						{
							pointOfInterestRoomIdentifiers.Add(room, pointOfInterestIdentifier);
							pointOfInterestLines.Add(new MMBRoomContentRecord(string.Format("{0}*:\n{1}", pointOfInterestIdentifier, room.PointOfInterestText)));
							pointOfInterestIdentifier++;
						}
					}
				}
				var pointOfInterestStartY = imageHeight + 10;
				(float height, float maxRequiredLineWidth) requiredSpaceForPointsOfInterest = (0, 0);

				if (pointOfInterestLines.Count > 0 && imageWidth > 100)
				{
					requiredSpaceForPointsOfInterest = SkiaMeasureMultilineText(paint, pointOfInterestLines, imageWidth - 10);

					imageHeight += 30 + (int)requiredSpaceForPointsOfInterest.height;
				}
				SKImageInfo imageInfo = new SKImageInfo(imageWidth,
														imageHeight);


				using (SKSurface surface = SKSurface.Create(imageInfo))
				{
					SKCanvas canvas = surface.Canvas;

					canvas.Clear(BackgroundColor.ToSKColor());
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
							else
							{
								paint.Color = room.FrameColor.ToSKColor();
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
											for (var i = x + 1; i < targetPos.X; ++i)
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
										paint.Color = ConnectionsColor.ToSKColor();
									}
								}
								else
								{
									paint.Color = ConnectionsColor.ToSKColor();
								}

								var sourceScreen = GetConnectionPoint(rect, exitDir);
								var targetRect = GetRoomRect(targetPos);
								var targetScreen = GetConnectionPoint(targetRect, exitDir.GetOppositeDirection());

								Point? startWithDoor = null, endWithDoor = null;
								if (ct == ConnectionType.Straight)
								{
									// Straight connection
									// Source and target room are close to each other, hence draw the simple line
									canvas.DrawLine(sourceScreen.X, sourceScreen.Y, targetScreen.X, targetScreen.Y, paint);

									if (pair.Value.IsDoor)
									{
										startWithDoor = sourceScreen;
										endWithDoor = targetScreen;
									}
								}
								else
								{
									List<Point> steps;

									Point mainLineStart, mainLineEnd;
									if (ct == ConnectionType.NonStraight)
									{
										steps = BuildNsPath(new Point(x, y), targetPos, exitDir, out mainLineStart, out mainLineEnd);
									}
									else
									{
										steps = BuildObstacledPath(new Point(x, y), targetPos, exitDir, out mainLineStart, out mainLineEnd);
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

									if (pair.Value.IsDoor)
									{
										startWithDoor = mainLineStart;
										endWithDoor = mainLineEnd;
									}
								}

								if (startWithDoor != null && endWithDoor != null)
								{
									// Draw door
									var src = startWithDoor.Value;
									var dest = endWithDoor.Value;

									// Normalized direction
									var v = new Vector2(dest.X - src.X, dest.Y - src.Y);
									v = Vector2.Normalize(v);

									// Make perpendecular to original
									v = new Vector2(v.Y, -v.X);

									// Determine center
									var center = new SKPoint(src.X + (dest.X - src.X) / 2,
										src.Y + (dest.Y - src.Y) / 2);

									var doorStart = new SKPoint(center.X - (v.X) * DoorSize / 2,
										center.Y - (v.Y) * DoorSize / 2);
									var doorEnd = new SKPoint(doorStart.X + v.X * DoorSize,
										doorStart.Y + v.Y * DoorSize);

									var oldColor = paint.Color;

									paint.Color = pair.Value.DoorColor.ToSKColor();
									canvas.DrawLine(doorStart.X, doorStart.Y, doorEnd.X, doorEnd.Y, paint);
									paint.Color = oldColor;

									if (pair.Value.DoorSigns != null)
									{
										var oldStrokeWidth = paint.StrokeWidth;
										paint.StrokeWidth = 1;

										var sz = SkiaMeasureMultilineText(paint, pair.Value.DoorSigns, 10000);

										center.X -= sz.maxRequiredLineWidth / 2.0f;
										center.Y -= (sz.height + 4.0f);

										SkiaDrawMultilineText(canvas, pair.Value.DoorSigns,
											new SKRect(center.X, center.Y, center.X + sz.maxRequiredLineWidth, center.Y + sz.height), paint);

										paint.StrokeWidth = oldStrokeWidth;
									}
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

							paint.StrokeWidth = 1;

							var hasPointOfInterest = pointOfInterestRoomIdentifiers.TryGetValue(room, out var roomPointOfInterest);

							var lines = new List<MMBRoomContentRecord>();
							lines.Add(new MMBRoomContentRecord(options.AddDebugInfo ? room.ToString() : room.Name, room.Color));

							if (hasPointOfInterest)
							{
								lines.Add(new MMBRoomContentRecord(roomPointOfInterest.ToString() + "*"));
							}

							if (room.Contents != null && room.Contents.Count > 0)
							{
								// Empty line before content
								lines.Add(new MMBRoomContentRecord());

								// Content
								lines.AddRange(room.Contents);
							}

							SkiaDrawMultilineText(canvas, lines, new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), paint);

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
									sourceScreen.Y + rect.Height / 2,
									targetScreen.X + addX,
									targetScreen.Y + rect.Height / 2, paint);
							}
						}
					}

					if (pointOfInterestLines.Count > 0)
					{
						paint.Color = SKColors.Black;
						paint.StrokeWidth = 1;
						paint.Style = SKPaintStyle.Stroke;
						var borderlocation = new SKRect(5, pointOfInterestStartY, imageWidth - 10, imageHeight - 10);
						var textlocation = new SKRect(5, pointOfInterestStartY, imageWidth - 10, imageHeight - 10);

						if (requiredSpaceForPointsOfInterest.maxRequiredLineWidth < imageWidth - 20)
						{
							var left = (imageWidth - requiredSpaceForPointsOfInterest.maxRequiredLineWidth) / 2;
							borderlocation = new SKRect(left - 5, pointOfInterestStartY + 5, left + requiredSpaceForPointsOfInterest.maxRequiredLineWidth + 5, imageHeight - 10);
							textlocation = new SKRect(left, pointOfInterestStartY + 5, left + requiredSpaceForPointsOfInterest.maxRequiredLineWidth, imageHeight - 15);
						}

						canvas.DrawRect(borderlocation, paint);

						paint.Style = SKPaintStyle.Fill;
						SkiaDrawMultilineText(canvas, pointOfInterestLines, textlocation, paint, centerOnLine: false);
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

		private List<Point> BuildNsPath(Point sourceGridPos, Point targetGridPos, MMBDirection direction, out Point mainLineStart, out Point mainLineEnd)
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
				mainLineStart = sourcePos;

				sourcePos = new Point(targetPos.X, sourcePos.Y);
				result.Add(sourcePos);
				mainLineEnd = sourcePos;
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
				mainLineStart = sourcePos;

				sourcePos = new Point(sourcePos.X, targetPos.Y);

				result.Add(sourcePos);
				mainLineEnd = sourcePos;
			}

			result.Add(targetPos);
			result.Add(targetConnectionPos);

			return result;
		}

		private List<Point> BuildObstacledPath(Point sourceGridPos, Point targetGridPos, MMBDirection direction, out Point mainLineStart, out Point mainLineEnd)
		{
			var sourceRoomRect = GetRoomRect(sourceGridPos);
			var sourcePos = GetConnectionPoint(sourceRoomRect, direction);

			int pathRadius;
			if (direction == MMBDirection.East || direction == MMBDirection.West)
			{
				pathRadius = RoomSpace.X / 4;
			}
			else
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
				mainLineStart = sourcePos;

				sourcePos = new Point(targetConnectionPos.X, sourcePos.Y);

				result.Add(sourcePos);
				mainLineEnd = sourcePos;
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
				mainLineStart = sourcePos;

				// Vertical movement
				sourcePos = new Point(sourcePos.X, targetConnectionPos.Y);

				result.Add(sourcePos);
				mainLineEnd = sourcePos;
			}

			return result;
		}

		private Point ToScreen(Point pos)
		{
			if (pos.X >= _cellsWidths.Length)
			{
				pos.X = _cellsWidths.Length - 1;
			}

			if (pos.Y >= _cellHeights.Length)
			{
				pos.Y = _cellHeights.Length - 1;
			}

			var screenX = RoomSpace.X;
			for (var x = 0; x < pos.X; ++x)
			{
				screenX += _cellsWidths[x];
				screenX += RoomSpace.X;
			}

			var screenY = RoomSpace.Y;
			for (var y = 0; y < pos.Y; ++y)
			{
				screenY += _cellHeights[y];
				screenY += RoomSpace.Y;
			}

			return new Point(screenX, screenY);
		}

		private Rectangle GetRoomRect(Point pos)
		{
			var screen = ToScreen(pos);
			return new Rectangle(screen.X, screen.Y, _cellsWidths[pos.X], _cellHeights[pos.Y]);
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

		private void SkiaDrawMultilineText(SKCanvas canvas, List<MMBRoomContentRecord> lines, SKRect rect, SKPaint paint, bool centerOnLine = true)
		{
			var oldColor = paint.Color;

			var (totalHeight, maxRequiredLineWidth) = SkiaMeasureMultilineText(paint, lines, (int)rect.Width);
			float wordY = paint.TextSize + rect.Top + (rect.Height / 2) - ((totalHeight - paint.FontSpacing) / 2);
			var spaceWidth = paint.MeasureText(" ");
			foreach (var line in lines)
			{
				List<string> lineWords = new List<string>();
				float lineWidth = 0;

				if (!string.IsNullOrEmpty(line.Text))
				{
					foreach (string word in line.Text.Split(' '))
					{
						var alteredword = word;
						float wordWidth = paint.MeasureText(word);
						if (wordWidth == 0)
						{
							wordWidth = spaceWidth;
							alteredword = " ";
						}
						if (lineWidth + wordWidth <= rect.Width)
						{
							lineWords.Add(alteredword);
							lineWidth += wordWidth + spaceWidth;
						}
						else
						{
							SkiaDrawLine(canvas, lineWords, rect, paint, ref wordY, centerOnLine);
							lineWords.Clear();
							lineWidth = 0;
							lineWords.Add(alteredword);
							lineWidth = wordWidth + spaceWidth;
						}
					}
				}

				if (lineWords.Count > 0)
				{
					paint.Color = line.Color.ToSKColor();
					SkiaDrawLine(canvas, lineWords, rect, paint, ref wordY, centerOnLine);
				}
				else
				{
					wordY += paint.FontSpacing;
				}
			}

			paint.Color = oldColor;
		}

		private void SkiaDrawLine(SKCanvas canvas, List<string> words, SKRect rect, SKPaint paint, ref float wordY, bool centerText = true)
		{
			string line = string.Join(" ", words);
			float lineWidth = paint.MeasureText(line);
			float startX = centerText ? rect.Left + rect.Width / 2 : rect.Left + lineWidth / 2;
			canvas.DrawText(line, startX, wordY, paint);
			wordY += paint.FontSpacing;
		}

		private (float height, float maxRequiredLineWidth) SkiaMeasureMultilineText(SKPaint paint, List<MMBRoomContentRecord> lines, int width)
		{
			float spaceWidth = paint.MeasureText(" ");
			float wordY = paint.TextSize;
			float maxRequiredLineWidth = 0;
			foreach (var line in lines)
			{
				float lineWidth = 0;

				if (!string.IsNullOrEmpty(line.Text))
				{
					foreach (string word in line.Text.Split(' '))
					{
						float wordWidth = paint.MeasureText(word);
						if (wordWidth == 0) wordWidth = spaceWidth;
						if (lineWidth + wordWidth <= width)
						{
							lineWidth += wordWidth + spaceWidth;
						}
						else
						{
							maxRequiredLineWidth = Math.Max(maxRequiredLineWidth, lineWidth - spaceWidth);
							wordY += paint.FontSpacing;
							lineWidth = wordWidth + spaceWidth;
						}
					}
				}

				maxRequiredLineWidth = Math.Max(maxRequiredLineWidth, lineWidth - spaceWidth);
				wordY += paint.FontSpacing;
			}
			return (wordY, maxRequiredLineWidth);
		}

	}
}