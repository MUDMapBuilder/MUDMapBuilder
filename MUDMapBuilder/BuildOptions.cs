using System.ComponentModel;

namespace MUDMapBuilder
{
	public class BuildOptions
	{
		public int? MaxSteps { get; set; } = null;

		public int? MaxCompactSteps { get; set; } = null;
		public bool RemoveSolitaryRooms { get; set; }
		public bool RemoveRoomsWithSingleOutsideExit { get; set; }


		public void CopyTo(BuildOptions other)
		{
			other.MaxSteps = MaxSteps;
			other.RemoveSolitaryRooms = RemoveSolitaryRooms;
			other.RemoveRoomsWithSingleOutsideExit = RemoveRoomsWithSingleOutsideExit;
		}
	}
}
