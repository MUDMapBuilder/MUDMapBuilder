using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
		private MMBArea _area;
		private MMBImageResult _imageResult;
		private bool _addDebugInfo, _colorizeConnectionIssues;

		public int? SelectedRoomId
		{
			get => _selectedRoomId;

			private set
			{
				if (value == _selectedRoomId) return;

				_selectedRoomId = value;
				_area.SelectedRoomId = value;
				SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler SelectedIndexChanged;

		public MapViewer()
		{
			Background = new SolidBrush(Color.White);
		}

		public void Redraw(MMBArea area, bool addDebugInfo, bool colorizeConnectionIssues)
		{
			_addDebugInfo = addDebugInfo;
			_colorizeConnectionIssues = colorizeConnectionIssues;

			Renderable = null;

			_area = area;
			if (_area == null)
			{
				return;
			}

			_imageResult = area.BuildPng(addDebugInfo, colorizeConnectionIssues);
			Texture2D texture;
			using (var ms = new MemoryStream(_imageResult.PngData))
			{
				texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
			}

			Renderable = new TextureRegion(texture);
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			if (_area == null)
			{
				return;
			}

			var pos = LocalTouchPosition.Value;
			foreach (var room in _imageResult.Rooms)
			{
				if (room.Rectangle.Contains(pos.X, pos.Y))
				{
					SelectedRoomId = room.Room.Id;
					Redraw(_area, _addDebugInfo, _colorizeConnectionIssues);
					break;
				}
			}
		}
	}
}