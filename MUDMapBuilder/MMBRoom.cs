using System.Drawing;

namespace MUDMapBuilder
{
	internal class MMBRoom
	{
		private Point _position;

		public int Id => Room.Id;

		public RoomsCollection Rooms { get; set; }
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

		public MMBRoom(IMMBRoom room)
		{
			Room = room;
		}

		public MMBRoom Clone() => new MMBRoom(Room)
		{
			Position = Position
		};

		public override string ToString() => $"{Room}, {Position}";
	}
}
