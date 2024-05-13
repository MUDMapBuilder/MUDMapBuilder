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

		public BuildOptions Clone()
		{
			return new BuildOptions
			{
				MaxSteps = MaxSteps,
			};
		}
	}
}
