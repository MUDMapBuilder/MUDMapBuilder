namespace MUDMapBuilder
{
	public class BuildOptions
	{
		public int? Steps { get; set; }

		/// <summary>
		/// Perform run to straighten rooms' connection
		/// </summary>
		public bool Straighten { get; set; } = true;

		/// <summary>
		/// Perform run to make the map more compact
		/// </summary>
		public bool Compact { get; set; } = true;

		public BuildOptions Clone()
		{
			return new BuildOptions
			{
				Steps = Steps,
				Straighten = Straighten,
				Compact = Compact
			};
		}
	}
}
