using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

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

	public class MMBRoomContentRecord
	{
		public string Text { get; set; }

		public Color? Color { get; set; } = null;

		public MMBRoomContentRecord()
		{
		}

		public MMBRoomContentRecord(string text, Color? color)
		{
			Text = text;
			Color = color;
		}

		public MMBRoomContentRecord(string text)
		{
			Text = text;
		}

		public override string ToString() => $"{Text}, {Color}";
	}

	public class MMBRoomConnection
	{
		[JsonIgnore]
		public MMBDirection Direction { get; set; }
		public int RoomId { get; set; }

		public Color? Color { get; set; } = null;

		public bool IsDoor { get; set; }
		public Color? DoorColor { get; set; } = null;

		public List<MMBRoomContentRecord> DoorSigns { get; set; }

		[JsonIgnore]
		public MMBConnectionType ConnectionType { get; set; }

		[JsonIgnore]
		public bool IsDoorWithKey => IsDoor && DoorSigns != null && DoorSigns.Count > 0;

		public MMBRoomConnection()
		{
		}

		public MMBRoomConnection(MMBDirection dir, int roomId)
		{
			Direction = dir;
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

		public int Id { get; set; }
		public Color? FrameColor { get; set; } = null;
		public string Name { get; set; }
		public Color? Color { get; set; } = null;

		public string PointOfInterestText { get; set; } = string.Empty;

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

		[JsonIgnore]
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

		[JsonIgnore]
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

		public Dictionary<MMBDirection, MMBRoomConnection> Connections { get; set; } = new Dictionary<MMBDirection, MMBRoomConnection>();
		public List<MMBRoomContentRecord> Contents { get; set; }

		[JsonIgnore]
		public object Tag { get; set; }

		public event EventHandler RoomInvalid;

		public MMBRoom()
		{
		}

		public MMBRoom(int id, string name) : this(id, name, null)
		{

		}

		public MMBRoom(int id, string name, string pointOfInterestText)
		{
			Id = id;
			Name = name;
			PointOfInterestText = pointOfInterestText;
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
			var result = new MMBRoom(Id, Name, PointOfInterestText)
			{
				FrameColor = FrameColor,
				Color = Color,
				Position = Position,
				MarkColor = MarkColor,
				ForceMark = ForceMark,
				Tag = Tag
			};

			foreach (var pair in Connections)
			{
				result.Connections[pair.Key] = pair.Value;
			}

			result.Contents = Contents;

			return result;
		}

		public override string ToString() => $"{Name}, {Position}";

		private void FireRoomInvalid() => RoomInvalid?.Invoke(this, EventArgs.Empty);
	}

}