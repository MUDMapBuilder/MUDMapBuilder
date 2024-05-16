using System.Collections.Generic;

namespace MUDMapBuilder.Editor.Data
{
	public class Room : IMMBRoom
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool IsExitToOtherArea { get; set; }

		public IReadOnlyDictionary<MMBDirection, IMMBRoom> Exits => InternalExits;

		public Dictionary<MMBDirection, IMMBRoom> InternalExits { get; } = new Dictionary<MMBDirection, IMMBRoom>();


		public override string ToString() => $"{Name} (#{Id})";
	}
}
