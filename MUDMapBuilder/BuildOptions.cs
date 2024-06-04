namespace MUDMapBuilder
{
	public class BuildOptions
	{
		public int MaxSteps { get; set; } = 1000;
		public bool RemoveSolitaryRooms { get; set; } = false;
		public bool FixObstacles { get; set; } = true;
		public bool FixNonStraight { get; set; } = true;
		public bool FixIntersected { get; set; } = true;
		public bool CompactMap { get; set; } = true;
		public bool AddDebugInfo { get; set; } = false;
		public bool ColorizeConnectionIssues { get; set; } = true;

		public void CopyTo(BuildOptions other)
		{
			other.MaxSteps = MaxSteps;
			other.FixObstacles = FixObstacles;
			other.FixNonStraight = FixNonStraight;
			other.FixIntersected = FixIntersected;
			other.CompactMap = CompactMap;
			other.AddDebugInfo = AddDebugInfo;
			other.ColorizeConnectionIssues = ColorizeConnectionIssues;
		}
	}
}
