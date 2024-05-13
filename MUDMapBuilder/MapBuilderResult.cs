namespace MUDMapBuilder
{
	public class MapBuilderResult
	{
		public RoomsCollection[] History { get; private set; }
		public RoomsCollection Last { get; private set; }
		public int TotalRooms { get; private set; }

		internal MapBuilderResult(RoomsCollection[] history, int totalRooms)
		{
			History = history;
			Last = history[history.Length - 1];
			TotalRooms = totalRooms;
		}
	}
}
