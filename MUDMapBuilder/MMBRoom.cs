using AbarimMUD.Data;
using System.Collections.Generic;
using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBRoom
	{
		private Dictionary<Direction, Point> _connections = new Dictionary<Direction, Point>();

		public Room Room { get; private set; }

		internal MMBRoom(Room room)
		{
			Room = room;
		}

		public void Connect(Direction direction, Point pos)
		{
			_connections[direction] = pos;
		}

		public bool IsConnected(Direction direction, Point pos)
		{
			Point connectedPos;
			if (!_connections.TryGetValue(direction, out connectedPos))
			{
				return false;
			}

			return connectedPos == pos;
		}
	}
}
