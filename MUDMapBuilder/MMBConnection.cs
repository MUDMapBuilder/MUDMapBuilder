namespace MUDMapBuilder
{
	public class MMBConnection
	{
		public int SourceRoomId { get; }
		public int TargetRoomId { get; }

		/// <summary>
		/// Direction from the source room to the target room
		/// </summary>
		public MMBDirection Direction { get; }

		/// <summary>
		/// Determines whether the connection is two-way(most rooms' connections are like that)
		/// </summary>
		public bool TwoWay { get; set; }

		public MMBConnection(int sourceRoomId, int destinationRoomId, MMBDirection direction)
		{
			SourceRoomId = sourceRoomId;
			TargetRoomId = destinationRoomId;
			Direction = direction;
		}
	}
}
