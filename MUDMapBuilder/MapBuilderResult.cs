namespace MUDMapBuilder
{
	public class MapBuilderResult
	{
		public PositionedRooms[] History { get; private set; }
		public PositionedRooms Last { get; private set; }
		public int StartCompactStep { get; private set; }
		internal MapBuilderResult(PositionedRooms[] history, int startCompactStep)
		{
			History = history;
			Last = history[history.Length - 1];
			StartCompactStep = startCompactStep;
		}
	}
}
