using SkiaSharp;
using System;
using System.Drawing;

namespace MUDMapBuilder
{
	internal static class Utility
	{
		public static readonly Random Random = new Random();

		public static Point GetDelta(this MMBDirection direction)
		{
			switch (direction)
			{
				case MMBDirection.East:
					return new Point(1, 0);
				case MMBDirection.West:
					return new Point(-1, 0);
				case MMBDirection.North:
					return new Point(0, -1);
				case MMBDirection.South:
					return new Point(0, 1);
				case MMBDirection.Up:
					return new Point(1, -1);
				case MMBDirection.Down:
					return new Point(-1, 1);
			}

			throw new Exception($"Unknown direction {direction}");
		}

		public static bool IsHorizontal(this MMBDirection direction) => direction == MMBDirection.East || direction == MMBDirection.West;
		public static bool IsVertical(this MMBDirection direction) => direction == MMBDirection.North || direction == MMBDirection.South;

		public static MMBDirection GetOppositeDirection(this MMBDirection direction)
		{
			switch (direction)
			{
				case MMBDirection.East:
					return MMBDirection.West;
				case MMBDirection.West:
					return MMBDirection.East;
				case MMBDirection.North:
					return MMBDirection.South;
				case MMBDirection.South:
					return MMBDirection.North;
				case MMBDirection.Up:
					return MMBDirection.Down;
				default:
					return MMBDirection.Up;
			}
		}

		public static int CalculateArea(this Rectangle r) => r.Width * r.Height;

		public static SKColor ToSKColor(this Color? color)
		{
			if (color == null)
			{
				return SKColors.Black;
			}

			return color.Value.ToSKColor();
		}

		public static SKColor ToSKColor(this Color color) => new SKColor(color.R, color.G, color.B, color.A);

		public static Color ToColor(this SKColor color) => Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
	}
}
