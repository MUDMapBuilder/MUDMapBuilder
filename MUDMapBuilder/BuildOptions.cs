using System;

namespace MUDMapBuilder
{
	public class BuildOptions
	{
		public int MaxSteps { get; set; } = 1000;
		public bool FixObstacles { get; set; } = true;
		public bool FixNonStraight { get; set; } = true;
		public bool FixIntersected { get; set; } = true;

		public void CopyTo(BuildOptions other)
		{
			other.MaxSteps = MaxSteps;
			other.FixObstacles = false;
			other.FixNonStraight = false;
			other.FixIntersected = false;
		}
	}
}
