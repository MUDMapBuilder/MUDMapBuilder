using System;

namespace MUDMapBuilder
{
	public enum AlgorithmUsage
	{
		DoNotUse,
		Use,
		LimitSteps
	}

	public class BuildOptions
	{
		public int MaxSteps { get; set; } = 1000;
		public bool FixObstacles { get; set; } = true;
		public bool FixNonStraight { get; set; } = true;
		public bool FixIntersected { get; set; } = true;
		public Action<string> Log { get; set; }

		public BuildOptions Clone()
		{
			return new BuildOptions
			{
				MaxSteps = MaxSteps,
			};
		}
	}
}
