using System.Drawing;

namespace MUDMapBuilder
{
	public class MMBImageRoomInfo
	{
		public IMMBRoom Room { get; private set; }
		public Rectangle Rectangle { get; private set; }

		internal MMBImageRoomInfo(IMMBRoom room, Rectangle rectangle)
		{
			Room = room;
			Rectangle = rectangle;
		}
	}

	public class MMBImageResult
	{
		public byte[] PngData { get; private set; }
		public MMBImageRoomInfo[] Rooms { get; private set; }

		internal MMBImageResult(byte[] pngData, MMBImageRoomInfo[] rooms)
		{
			PngData = pngData;
			Rooms = rooms;
		}
	}
}
