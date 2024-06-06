using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	public enum MMBDirection
	{
		North,
		East,
		South,
		West,
		Up,
		Down,
	}

	public enum MMBConnectionType
	{
		Forward,
		Backward,
		TwoWay
	}

	public class MMBRoomConnection
	{
		public MMBDirection Direction { get; private set; }
		public int RoomId { get; private set; }
		public MMBConnectionType ConnectionType { get; internal set; }

		public MMBRoomConnection(MMBDirection direction, int roomId)
		{
			Direction = direction;
			RoomId = roomId;
		}

		public override string ToString() => $"{RoomId}, {ConnectionType}";
	}

	public class MMBRoom
	{
		private Dictionary<MMBDirection, Point> _drawnConnections = new Dictionary<MMBDirection, Point>();
		internal Point? _position;
		private SKColor? _markColor;
		private Point? _forceMark;

		public int Id { get; private set; }
		public string Name { get; set; }
		public bool IsExitToOtherArea { get; set; }

		public Point? Position
		{
			get => _position;
			set
			{
				if (_position == value)
				{
					return;
				}

				_position = value;
				FireRoomInvalid();
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
				FireRoomInvalid();
			}
		}

		public Point? ForceMark
		{
			get => _forceMark;
			set
			{
				if (value == _forceMark)
				{
					return;
				}

				_forceMark = value;
				FireRoomInvalid();
			}
		}

		public Dictionary<MMBDirection, MMBRoomConnection> Connections { get; } = new Dictionary<MMBDirection, MMBRoomConnection>();

		public event EventHandler RoomInvalid;

		public MMBRoom(int id, string name, bool isExitToOtherArea)
		{
			Id = id;
			Name = name;
			IsExitToOtherArea = isExitToOtherArea;
		}

		public MMBRoomConnection FindConnection(int targetRoomId)
		{
			return (from pair in Connections where pair.Value.RoomId == targetRoomId select pair.Value).FirstOrDefault();
		}

		internal void ClearDrawnConnections() => _drawnConnections.Clear();

		internal void AddDrawnConnection(MMBDirection direction, Point pos)
		{
			_drawnConnections[direction] = pos;
		}

		internal bool HasDrawnConnection(MMBDirection direction, Point pos)
		{
			Point connectedPos;
			if (!_drawnConnections.TryGetValue(direction, out connectedPos))
			{
				return false;
			}

			return connectedPos == pos;
		}

		public MMBRoom Clone()
		{
			var result = new MMBRoom(Id, Name, IsExitToOtherArea)
			{
				Position = Position,
				MarkColor = MarkColor,
				ForceMark = ForceMark,
			};

			foreach (var pair in Connections)
			{
				result.Connections[pair.Key] = pair.Value;
			}

			return result;
		}

		public override string ToString() => $"{Name} (#{Id}), {Position}";

		private void FireRoomInvalid() => RoomInvalid?.Invoke(this, EventArgs.Empty);
	}

}