using System.Drawing;

namespace MUDMapBuilder
{
	public class MeasurePushRoomMovement
	{
		public MMBRoom Room { get; private set; }
		public Point Delta { get; private set; }

		internal MeasurePushRoomMovement(MMBRoom room, Point delta)
		{
			Room = room;
			Delta = delta;
		}
	}

	public class MeasurePushRoomResult
	{
		public MeasurePushRoomMovement[] MovedRooms { get; private set; }
		public MMBRoom[] DeletedRooms { get; private set; }

		internal MeasurePushRoomResult(MeasurePushRoomMovement[] movedRooms, MMBRoom[] deletedRooms)
		{
			MovedRooms = movedRooms;
			DeletedRooms = deletedRooms;
		}
	}
}
