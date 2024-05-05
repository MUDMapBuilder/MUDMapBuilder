namespace MUDMapBuilder
{
	public enum MMBDirection
	{
		North,
		East,
		South,
		West,
		Up,
		Down,
	}

	public interface IMMBRoom
	{
		int Id { get; }
		string Name { get; }
		MMBDirection[] ExitsDirections { get; }
		IMMBRoom GetRoomByExit(MMBDirection direction);
	}
}
