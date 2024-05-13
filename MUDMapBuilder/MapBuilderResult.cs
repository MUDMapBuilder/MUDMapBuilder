namespace MUDMapBuilder
{
	public class MapBuilderResult
	{
		public PositionedRooms[] History { get; private set; }
		public PositionedRooms Last { get; private set; }
		public int TotalRooms { get; private set; }

		internal MapBuilderResult(PositionedRooms[] history, int totalRooms)
		{
			History = history;
			Last = history[history.Length - 1];
			TotalRooms = totalRooms;
		}
	}
}
