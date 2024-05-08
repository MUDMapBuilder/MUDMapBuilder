namespace MUDMapBuilder
{
	public class BrokenConnectionsInfo
	{
		public int NonStraightConnectionsCount { get; }
		public int ConnectionsWithObstaclesCount { get; }
		public int Count => NonStraightConnectionsCount + ConnectionsWithObstaclesCount;

		internal BrokenConnectionsInfo(int nonStraightConnectionsCount, int connectionsWithObstaclesCount)
		{
			NonStraightConnectionsCount = nonStraightConnectionsCount;
			ConnectionsWithObstaclesCount = connectionsWithObstaclesCount;
		}
	}
}
