namespace MUDMapBuilder
{
	public class BrokenConnectionsInfo
	{
		public MMBConnectionsList Normal { get; } = new MMBConnectionsList();
		public MMBConnectionsList WithObstacles { get; } = new MMBConnectionsList();
		public MMBConnectionsList NonStraight { get; } = new MMBConnectionsList();
		public MMBConnectionsList Long { get; } = new MMBConnectionsList();
	}
}
