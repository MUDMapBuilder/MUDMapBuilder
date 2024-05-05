using AbarimMUD.Data;
using System;

namespace MUDMapBuilder.Sample
{
	internal static class Utility
	{
		public static MMBDirection ToMBBDirection(this Direction direction)
		{
			switch (direction)
			{
				case Direction.North:
					return MMBDirection.North;
				case Direction.East:
					return MMBDirection.East;
				case Direction.South:
					return MMBDirection.South;
				case Direction.West:
					return MMBDirection.West;
				case Direction.Up:
					return MMBDirection.Up;
				case Direction.Down:
					return MMBDirection.Down;
			}

			throw new Exception($"Unknown direction {direction}");
		}
	}
}
