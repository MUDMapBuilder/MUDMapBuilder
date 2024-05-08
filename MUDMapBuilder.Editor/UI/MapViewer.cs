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
		private MMBGrid _grid;
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
			_grid = MapBuilder.BuildGrid(rooms, maxSteps, compactRuns);
			if (maxSteps == null)
			{
				MaxSteps = _grid.Steps;
			}

			Redraw();
		}

		private void Redraw()
		{
			_imageResult = _grid.BuildPng();
			using (var ms = new MemoryStream(_imageResult.PngData))
			{
				var texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
				Renderable = new TextureRegion(texture);
			}
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			var pos = LocalTouchPosition.Value;
			foreach(var room in _imageResult.Rooms)
			{
				if (room.Rectangle.Contains(pos.X, pos.Y))
				{
					_grid.SelectedRoomId = room.Room.Id;
					Redraw();
					break;
				}
			}
		}
	}
}