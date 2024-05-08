using System;
using System.Drawing;

namespace MUDMapBuilder
{
	internal static class Utility
	{
		public static readonly Random Random = new Random();

		public static Point GetDelta(this MMBDirection direction)
		{
			switch (direction)
			{
				case MMBDirection.East:
					return new Point(1, 0);
				case MMBDirection.West:
					return new Point(-1, 0);
				case MMBDirection.North:
					return new Point(0, -1);
				case MMBDirection.South:
					return new Point(0, 1);
				case MMBDirection.Up:
					return new Point(1, -1);
				case MMBDirection.Down:
					return new Point(-1, 1);
			}

			throw new Exception($"Unknown direction {direction}");
		}

		public static MMBDirection GetOppositeDirection(this MMBDirection direction)
		{
			switch (direction)
			{
				case MMBDirection.East:
					return MMBDirection.West;
				case MMBDirection.West:
					return MMBDirection.East;
				case MMBDirection.North:
					return MMBDirection.South;
				case MMBDirection.South:
					return MMBDirection.North;
				case MMBDirection.Up:
					return MMBDirection.Down;
				default:
					return MMBDirection.Up;
			}
		}

		public static bool IsConnectedTo(this IMMBRoom room1, IMMBRoom room2)
		{
			var exitDirs = room1.ExitsDirections;
			for (var i = 0; i < exitDirs.Length; ++i)
			{
				var exitDir = exitDirs[i];
				var exitRoom = room1.GetRoomByExit(exitDir);

				if (exitRoom.Id == room2.Id)
				{
					return true;
				}
			}

			return false;
		}

		public static int CalculateArea(this Rectangle r) => r.Width * r.Height;
	}
}
