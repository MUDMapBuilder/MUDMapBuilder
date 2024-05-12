using System.Collections.Generic;
using System;
using System.Drawing;
using SkiaSharp;

namespace MUDMapBuilder
{
	public class MMBCell
	{
		public Point Position { get; }

		internal MMBCell(Point position)
		{
			Position = position;
		}
	}

	/// <summary>
	/// Grid cell with a room
	/// </summary>
	public class MMBRoomCell : MMBCell
	{
		private Dictionary<MMBDirection, Point> _drawnConnections = new Dictionary<MMBDirection, Point>();


		public IMMBRoom Room { get; }
		public int Id => Room.Id;
		public SKColor? MarkColor { get; internal set; }
		internal Point? ForceMark { get; set; } = null;

		internal MMBRoomCell(IMMBRoom room, Point position) : base(position)
		{
			Room = room;
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

		public override string ToString() => $"{Room}, {Position}";
	}

	/// <summary>
	/// Grid cell with rooms' connections
	/// </summary>
	public class MMBConnectionsCell : MMBCell
	{
		private List<Tuple<MMBRoomCell, MMBRoomCell>> _rooms = new List<Tuple<MMBRoomCell, MMBRoomCell>>();

		public int Count => _rooms.Count;

		public Tuple<MMBRoomCell, MMBRoomCell> this[int index] => _rooms[index];

		internal MMBConnectionsCell(Point position) : base(position)
		{
		}

		public bool HasPair(int roomId1, int roomId2)
		{
			foreach (var r in _rooms)
			{
				if ((r.Item1.Room.Id == roomId1 && r.Item2.Room.Id == roomId2) ||
					(r.Item1.Room.Id == roomId2 && r.Item2.Room.Id == roomId1))
				{
					// Already added
					return true;
				}
			}

			return false;
		}

		internal void AddPair(MMBRoomCell room1, MMBRoomCell room2)
		{
			if (HasPair(room1.Room.Id, room2.Room.Id))
			{
				return;
			}

			_rooms.Add(new Tuple<MMBRoomCell, MMBRoomCell>(room1, room2));
		}
	}
}
