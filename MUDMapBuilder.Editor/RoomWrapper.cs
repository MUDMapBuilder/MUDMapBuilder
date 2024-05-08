using AbarimMUD.Data;
using System.Linq;

namespace MUDMapBuilder.Editor
{
	internal class RoomWrapper : IMMBRoom
	{
		private readonly Room _room;

		public RoomWrapper(Room room)
		{
			_room = room;
		}

		public int Id => _room.Id;

		public string Name => _room.Name;

		public MMBDirection[] ExitsDirections => (from ex in _room.Exits where ex.TargetRoom != null && ex.TargetRoom.AreaId == _room.AreaId select ex.Direction.ToMBBDirection()).ToArray();

		public IMMBRoom GetRoomByExit(MMBDirection direction)
		{
			var targetRoom = (from ex in _room.Exits where ex.Direction.ToMBBDirection() == direction select ex.TargetRoom).First();

			return new RoomWrapper(targetRoom);
		}

		public override string ToString() => _room.ToString();
	}
}
