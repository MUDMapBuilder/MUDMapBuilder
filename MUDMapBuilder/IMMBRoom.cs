using System.Collections.Generic;

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
		public IReadOnlyDictionary<MMBDirection, IMMBRoom> Exits { get; }
	}
}
