using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MUDMapBuilder.Editor.Data;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.IO;


namespace MUDMapBuilder.Editor.UI
{
	public class MapViewer : Image
	{
		private int? _selectedRoomId;
		private Area _area;
		private MMBImageResult _imageResult;
		private int _step;

		public int? SelectedRoomId
		{
			get => _selectedRoomId;

			private set
			{
				if (value == _selectedRoomId) return;

				_selectedRoomId = value;
				Rooms.SelectedRoomId = value;
				SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public MapBuilderResult Result { get; private set; }

		public int Step
		{
			get => _step;

			set
			{
				_step = value;
				Redraw();
			}
		}

		public PositionedRooms Rooms
		{
			get
			{
				if (Result == null)
				{
					return null;
				}

				var step = Math.Min(Result.History.Length - 1, Step);
				return Result.History[step];
			}
		}

		public Area Area
		{
			get => _area;

			set
			{
				if (value == _area)
				{
					return;
				}

				_area = value;
				Rebuild(new BuildOptions());
			}
		}

		public event EventHandler SelectedIndexChanged;

		public MapViewer()
		{
			Background = new SolidBrush(Color.White);
		}

		public void Rebuild(BuildOptions options)
		{
			Result = null;
			_imageResult = null;

			if (Area != null)
			{
				Result = MapBuilder.Build(Area.Rooms.ToArray(), options);
				Step = Result.History.Length - 1;
			}

			Redraw();
		}

		public void Redraw()
		{
			Renderable = null;

			if (Result != null)
			{
				var rooms = Rooms;
				_imageResult = rooms.BuildPng();
				using (var ms = new MemoryStream(_imageResult.PngData))
				{
					var texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
					Renderable = new TextureRegion(texture);
				}
				foreach (var room in rooms)
				{
					room.MarkColor = null;
				}
			}
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			if (Rooms == null)
			{
				return;
			}

			var pos = LocalTouchPosition.Value;
			foreach (var room in _imageResult.Rooms)
			{
				if (room.Rectangle.Contains(pos.X, pos.Y))
				{
					SelectedRoomId = room.Room.Id;
					Redraw();
					break;
				}
			}
		}
	}
}