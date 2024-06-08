namespace MUDMapBuilder
{
	public enum ResultType
	{
		Success,
		OutOfSteps,
		MapTooBig
	}

	public class MapBuilderResult
	{
		public ResultType ResultType { get; private set; }
		public MMBArea[] History { get; private set; } = new MMBArea[0];
		public MMBArea Last { get; private set; }
		public int StartCompactStep { get; private set; }
		internal MapBuilderResult(ResultType resultType, MMBArea[] history, int startCompactStep)
		{
			ResultType = resultType;
			History = history;
			Last = history[history.Length - 1];
			StartCompactStep = startCompactStep;
		}
	}
}
