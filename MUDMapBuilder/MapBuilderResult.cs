namespace MUDMapBuilder
{
	public class MapBuilderResult
	{
		public PositionedRooms[] History { get; private set; }
		public PositionedRooms Last { get; private set; }
		internal MapBuilderResult(PositionedRooms[] history)
		{
			History = history;
			Last = history[history.Length - 1];
		}
	}
}
