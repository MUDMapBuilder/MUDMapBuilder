using SkiaSharp;
using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBRoom
	{
		private Point _position;
		private Point? _forceMark;

		public int Id => Room.Id;

		public RoomsCollection Rooms { get; internal set; }
		public IMMBRoom Room { get; }
		public Point Position
		{
			get => _position;
			set
			{
				if (_position == value)
				{
					return;
				}

				_position = value;
				if (Rooms != null)
				{
					Rooms.InvalidateGrid();
				}
			}
		}
		public SKColor? MarkColor { get; set; }
		internal Point? ForceMark
		{
			get => _forceMark;
			set
			{
				if (value == _forceMark)
				{
					return;
				}

				_forceMark = value;
				if (Rooms != null)
				{
					Rooms.InvalidateGrid();
				}
			}
		}

		public MMBRoom(IMMBRoom room)
		{
			Room = room;
		}

		public MMBRoom Clone() => new MMBRoom(Room)
		{
			Position = Position,
			MarkColor = MarkColor,
			ForceMark = ForceMark,
		};

		public override string ToString() => $"{Room}, {Position}";
	}
}
