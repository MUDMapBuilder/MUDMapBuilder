using AbarimMUD.Data;
using Microsoft.Xna.Framework;
using System;

namespace MUDMapBuilder.Sample
{
	internal static class Utility
	{
		public static readonly Random Random = new Random();

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

		public static Vector2 GetDelta(this Direction direction)
		{
			switch (direction)
			{
				case Direction.East:
					return new Vector2(1, 0);
				case Direction.West:
					return new Vector2(-1, 0);
				case Direction.North:
					return new Vector2(0, -1);
				case Direction.South:
					return new Vector2(0, 1);
				case Direction.Up:
					return new Vector2(1, -1);
				case Direction.Down:
					return new Vector2(-1, 1);
			}

			throw new Exception($"Unknown direction {direction}");
		}
	}
}
