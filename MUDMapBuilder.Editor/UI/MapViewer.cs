using AbarimMUD.Data;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System.IO;
using System.Linq;

namespace MUDMapBuilder.Editor.UI
{
	public class MapViewer : Image
	{
		private Area _map;
		private RoomsCollection _rooms;
		private MMBImageResult _imageResult;

		public Area Map
		{
			get => _map;

			set
			{
				if (value == _map)
				{
					return;
				}

				_map = value;
				Rebuild();
			}
		}

		public int MaxSteps { get; private set; }

		public MapViewer()
		{
		}

		public void Rebuild(int? maxSteps = null, int? compactRuns = null)
		{
			var rooms = (from r in _map.Rooms select new RoomWrapper(r)).ToArray();
			_rooms = MapBuilder.Build(rooms, maxSteps, compactRuns);
			if (maxSteps == null)
			{
				MaxSteps = _rooms.Steps;
			}

			Redraw();
		}

		private void Redraw()
		{
			_imageResult = _rooms.Grid.BuildPng();
			using (var ms = new MemoryStream(_imageResult.PngData))
			{
				var texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
				Renderable = new TextureRegion(texture);
			}

			foreach(var room in _rooms)
			{
				room.Mark = false;
			}
		}

		public void MeasurePushRoom(MMBDirection direction)
		{
			var selectedRoomId = _rooms.Grid.SelectedRoomId;
			if (selectedRoomId == null)
			{
				return;
			}

			var room = _rooms.GetRoomById(selectedRoomId.Value);
			var roomsToMark = _rooms.MeasurePushRoom(room, direction);
			foreach(var pair in roomsToMark)
			{
				pair.Value.Mark = true;
			}

			_rooms.InvalidateGrid();

			Redraw();
		}

		public void PushRoom(MMBDirection direction)
		{
			var selectedRoomId = _rooms.Grid.SelectedRoomId;
			if (selectedRoomId == null)
			{
				return;
			}

			var room = _rooms.GetRoomById(selectedRoomId.Value);
			_rooms.PushRoom(room, direction, 1);

			Redraw();
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			var pos = LocalTouchPosition.Value;
			foreach (var room in _imageResult.Rooms)
			{
				if (room.Rectangle.Contains(pos.X, pos.Y))
				{
					_rooms.SelectedRoomId = room.Room.Id;
					Redraw();
					break;
				}
			}
		}
	}
}