namespace MUDMapBuilder
{
	public class MapBuilderResult
	{
		public MMBArea[] History { get; private set; }
		public MMBArea Last { get; private set; }
		public int StartCompactStep { get; private set; }
		internal MapBuilderResult(MMBArea[] history, int startCompactStep)
		{
			History = history;
			Last = history[history.Length - 1];
			StartCompactStep = startCompactStep;
		}
	}
}
