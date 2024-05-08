using System.Collections.Generic;
using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBRoom
	{
		private Dictionary<MMBDirection, Point> _connections = new Dictionary<MMBDirection, Point>();

		public int Id => Room.Id;
		public IMMBRoom Room { get; private set; }
		public Point Position { get; internal set; }

		internal MMBRoom(IMMBRoom room)
		{
			Room = room;
		}

		internal void ClearConnections() => _connections.Clear();

		internal void Connect(MMBDirection direction, Point pos)
		{
			_connections[direction] = pos;
		}

		internal bool IsConnected(MMBDirection direction, Point pos)
		{
			Point connectedPos;
			if (!_connections.TryGetValue(direction, out connectedPos))
			{
				return false;
			}

			return connectedPos == pos;
		}

		public MMBRoom Clone() => new MMBRoom(Room)
		{
			Position = Position
		};

		public override string ToString() => $"{Room}, {Position}";
	}
}
