using AbarimMUD.Data;
using System;
using System.Drawing;
using System.Text;

namespace MUDMapBuilder
{
	public partial class MMBGrid
	{
		private readonly MMBRoom[,] _cellData;

		public MMBRoom this[Point coord]
		{
			get => _cellData[coord.X, coord.Y];
			set => _cellData[coord.X, coord.Y] = value;
		}

		public MMBRoom this[int x, int y]
		{
			get => _cellData[x, y];
			set => _cellData[x, y] = value;
		}

		public int Width => _cellData.GetLength(0);
		public int Height => _cellData.GetLength(1);

		public int Steps { get; }

		internal MMBGrid(Point size, int steps)
		{
			_cellData = new MMBRoom[size.X, size.Y];
			Steps = steps;
		}

		public bool AreRoomsConnected(Point a, Point b, Direction direction)
		{
			var room = this[a];
			if (room.IsConnected(direction, b))
			{
				return true;
			}

			room = this[b];
			if (room.IsConnected(direction.GetOppositeDirection(), a))
			{
				return true;
			}

			return false;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			for (var y = 0; y < Height; ++y)
			{
				for (var x = 0; x < Width; ++x)
				{
					sb.Append(_cellData[x, y] != null ? "x" : ".");
				}

				sb.Append(Environment.NewLine);
			}

			return sb.ToString();
		}
	}

	internal static class MMBGridExtensions
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
	}
}
