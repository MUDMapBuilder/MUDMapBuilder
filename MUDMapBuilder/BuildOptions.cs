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

		/// <summary>
		/// Perform run to straighten rooms' connection
		/// </summary>
		public AlgorithmUsage StraightenUsage { get; set; } = AlgorithmUsage.Use;

		public int StraightenSteps { get; set; }

		internal bool EndStraighten(int steps)
		{
			if (StraightenUsage != AlgorithmUsage.LimitSteps)
			{
				return false;
			}

			return steps >= StraightenSteps;
		}

		public BuildOptions Clone()
		{
			return new BuildOptions
			{
				MaxSteps = MaxSteps,
				StraightenUsage = StraightenUsage,
				StraightenSteps = StraightenSteps,
			};
		}
	}
}
