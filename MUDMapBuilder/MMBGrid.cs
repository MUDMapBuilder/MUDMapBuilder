using System;
using System.Drawing;
using System.Text;

namespace MUDMapBuilder
{
	public partial class MMBGrid
	{
		private readonly MMBCell[,] _cellData;

		public MMBCell this[Point coord]
		{
			get => _cellData[coord.X, coord.Y];
			set => _cellData[coord.X, coord.Y] = value;
		}

		public MMBCell this[int x, int y]
		{
			get => _cellData[x, y];
			set => _cellData[x, y] = value;
		}

		public int Width => _cellData.GetLength(0);
		public int Height => _cellData.GetLength(1);

		public int? SelectedRoomId { get; internal set; }

		internal MMBGrid(int width, int height)
		{
			_cellData = new MMBCell[width, height];
		}

		public MMBRoomCell GetRoomById(int id)
		{
			for (var x = 0; x < Width; ++x)
			{
				for (var y = 0; y < Height; ++y)
				{
					var asRoom = this[x, y] as MMBRoomCell;
					if (asRoom == null)
					{
						continue;
					}

					if (asRoom.Id == id)
					{
						return asRoom;
					}
				}
			}

			return null;
		}

		public bool AreRoomsConnected(Point a, Point b, MMBDirection direction)
		{
			var room = (MMBRoomCell)this[a];
			if (room.HasDrawnConnection(direction, b))
			{
				return true;
			}

			room = (MMBRoomCell)this[b];
			if (room.HasDrawnConnection(direction.GetOppositeDirection(), a))
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
}