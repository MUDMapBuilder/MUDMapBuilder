using System.Collections;
using System.Collections.Generic;

namespace MUDMapBuilder
{
	public class MMBConnectionsList : IEnumerable<MMBConnection>
	{
		private readonly List<MMBConnection> _connections = new List<MMBConnection>();

		public int Count => _connections.Count;
		public MMBConnection this[int index] => _connections[index];

		public MMBConnection Find(int sourceRoomId, int targetRoomId)
		{
			foreach (var connection in _connections)
			{
				if (connection.SourceRoomId == sourceRoomId && connection.TargetRoomId == targetRoomId)
				{
					return connection;
				}

				// Check existance of opposite connection
				if (connection.SourceRoomId == targetRoomId && connection.TargetRoomId == sourceRoomId)
				{
					return connection;
				}
			}

			return null;
		}

		public MMBConnection Find(int sourceRoomId, int targetRoomId, MMBDirection direction)
		{
			foreach (var connection in _connections)
			{
				if (connection.SourceRoomId == sourceRoomId &&
					connection.TargetRoomId == targetRoomId &&
					connection.Direction == direction)
				{
					return connection;
				}

				// Check existance of opposite connection
				if (connection.SourceRoomId == targetRoomId &&
					connection.TargetRoomId == sourceRoomId &&
					connection.Direction == direction.GetOppositeDirection())
				{
					return connection;
				}
			}

			return null;
		}

		public MMBConnection Add(int sourceRoomId, int targetRoomId, MMBDirection direction)
		{
			// Check if such connection already exist
			var connection = Find(sourceRoomId, targetRoomId, direction);
			if (connection != null)
			{
				if (connection.Direction == direction.GetOppositeDirection())
				{
					// Opposite connection exists, mark it as two-way
					connection.TwoWay = true;
				}

				return connection;
			}

			connection = new MMBConnection(sourceRoomId, targetRoomId, direction);
			_connections.Add(connection);

			return connection;
		}

		public void Remove(MMBConnection connection) => _connections.Remove(connection);

		public void RemoveAt(int index) => _connections.RemoveAt(index);

		public IEnumerator<MMBConnection> GetEnumerator() => _connections.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _connections.GetEnumerator();
	}
}
