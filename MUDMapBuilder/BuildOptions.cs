namespace MUDMapBuilder
{
	public class BuildOptions
	{
		public int MaxSteps { get; set; } = 1000;
		public int MaxCompactSteps { get; set; } = 10000;
		public bool RemoveSolitaryRooms { get; set; }
		public bool RemoveRoomsWithSingleOutsideExit { get; set; }
		public bool AddDebugInfo { get; set; }


		public void CopyTo(BuildOptions other)
		{
			other.MaxSteps = MaxSteps;
			other.RemoveSolitaryRooms = RemoveSolitaryRooms;
			other.RemoveRoomsWithSingleOutsideExit = RemoveRoomsWithSingleOutsideExit;
			other.AddDebugInfo = AddDebugInfo;
		}
	}
}
