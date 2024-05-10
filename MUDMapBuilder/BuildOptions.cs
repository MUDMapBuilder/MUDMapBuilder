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
		public int? Steps { get; set; }

		/// <summary>
		/// Perform run to straighten rooms' connection
		/// </summary>
		public AlgorithmUsage StraightenUsage { get; set; } = AlgorithmUsage.Use;

		public int StraightenSteps { get; set; }

		/// <summary>
		/// Perform run to make the map more compact
		/// </summary>
		public AlgorithmUsage CompactUsage { get; set; } = AlgorithmUsage.Use;

		public int CompactSteps { get; set; }


		internal bool EndStraighten(int maxSteps)
		{
			if (StraightenUsage != AlgorithmUsage.LimitSteps)
			{
				return false;
			}

			return maxSteps >= StraightenSteps;
		}

		public BuildOptions Clone()
		{
			return new BuildOptions
			{
				Steps = Steps,
				StraightenUsage = StraightenUsage,
				StraightenSteps = StraightenSteps,
				CompactUsage = CompactUsage,
				CompactSteps = CompactSteps
			};
		}
	}
}
