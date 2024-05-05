using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MUDMapBuilder
{
	internal class RoomsCollection: List<MMBRoom>
	{
		public void PushRoom(Point pos, MMBDirection dir)
		{
			foreach (var room in this)
			{
				var roomPos = room.Position;
				switch (dir)
				{
					case MMBDirection.North:
						if (roomPos.Y <= pos.Y)
						{
							--roomPos.Y;
						}
						break;
					case MMBDirection.East:
						if (roomPos.X >= pos.X)
						{
							++roomPos.X;
						}
						break;
					case MMBDirection.South:
						if (roomPos.Y >= pos.Y)
						{
							++roomPos.Y;
						}
						break;
					case MMBDirection.West:
						if (roomPos.X <= pos.X)
						{
							--roomPos.X;
						}
						break;
					case MMBDirection.Up:
						if (roomPos.Y <= pos.Y || roomPos.X >= pos.X)
						{
							--roomPos.Y;
							++roomPos.X;
						}
						break;
					case MMBDirection.Down:
						if (roomPos.Y >= pos.Y || roomPos.X <= pos.X)
						{
							++roomPos.Y;
							--roomPos.X;
						}
						break;
				}

				room.Position = roomPos;
			}
		}

		public bool HasOverlaps()
		{
			foreach(var room in this)
			{
				foreach(var room2 in this)
				{
					if (room == room2)
					{
						continue;
					}

					if (room.Position == room2.Position)
					{
						return true;
					}
				}
			}

			return false;
		}

		public MMBRoom GetRoomById(int id) => (from r in this where r.Room.Id == id select r).FirstOrDefault();
		public MMBRoom GetRoomByPosition(Point pos) => (from r in this where r.Position == pos select r).FirstOrDefault();

		public void PullRoom(MMBRoom firstRoom, MMBDirection direction, int steps)
		{
			var initialPos = firstRoom.Position;
			var delta = direction.GetDelta();

			var toProcess = new List<MMBRoom>
			{
				firstRoom
			};

			var processed = new List<MMBRoom>();

			while(toProcess.Count > 0)
			{
				var room = toProcess[0];
				toProcess.RemoveAt(0);
				processed.Add(room);

				var pos = room.Position;
				pos.X += delta.X * steps;
				pos.Y += delta.Y * steps;
				room.Position = pos;

				var exitDirs = room.Room.ExitsDirections;
				for (var i = 0; i < exitDirs.Length; ++i)
				{
					var exitDir = exitDirs[i];
					var exitRoom = room.Room.GetRoomByExit(exitDir);
					var targetRoom = GetRoomById(exitRoom.Id);
					if (targetRoom == null || toProcess.Contains(targetRoom) || processed.Contains(targetRoom))
					{
						continue;
					}

					var targetPos = targetRoom.Position;
					var add = false;
					switch (direction)
					{
						case MMBDirection.North:
							add = targetPos.Y >= initialPos.Y;
							break;
						case MMBDirection.East:
							add = targetPos.X <= initialPos.X;
							break;
						case MMBDirection.South:
							add = targetPos.Y <= initialPos.Y;
							break;
						case MMBDirection.West:
							add = targetPos.X >= initialPos.X;
							break;
						case MMBDirection.Up:
						case MMBDirection.Down:
							throw new NotImplementedException();
					}

					if (add)
					{
						toProcess.Add(targetRoom);
					}
				}
			}
		}

		public RoomsCollection Clone()
		{
			var result = new RoomsCollection();
			foreach(var r in this)
			{
				result.Add(r.Clone());
			}

			return result;
		}
	}
}
