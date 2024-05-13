using SkiaSharp;
using System;
using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBRoom
	{
		private Point _position;
		private SKColor? _markColor;
		private Point? _forceMark;

		public int Id => Room.Id;

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
				FireInvalid();
			}
		}
		public SKColor? MarkColor
		{
			get => _markColor;

			set
			{
				if (value == _markColor)
				{
					return;
				}

				_markColor = value;
				FireInvalid();
			}
		}

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
				FireInvalid();
			}
		}

		public event EventHandler Invalid;

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

		private void FireInvalid() => Invalid?.Invoke(this, EventArgs.Empty);
	}
}
