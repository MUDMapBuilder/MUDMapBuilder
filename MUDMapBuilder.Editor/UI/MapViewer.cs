using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MUDMapBuilder.Editor.Data;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using SkiaSharp;
using System;
using System.IO;

using Point = System.Drawing.Point;

namespace MUDMapBuilder.Editor.UI
{
	public class MapViewer : Image
	{
		private RoomsCollection _rooms;
		private MMBImageResult _imageResult;

		public Area Area { get; set; }

		public RoomsCollection Rooms => _rooms;
		
		public MapViewer()
		{
			Background = new SolidBrush(Color.White);
		}

		public void Rebuild(BuildOptions options)
		{
			_rooms = null;
			_imageResult = null;

			if (Area != null)
			{
				_rooms = MapBuilder.Build(Area.Rooms.ToArray(), options);
			}

			Redraw();
		}

		private void Redraw()
		{
			Renderable = null;

			if (_rooms != null)
			{
				_imageResult = _rooms.Grid.BuildPng();
				using (var ms = new MemoryStream(_imageResult.PngData))
				{
					var texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
					Renderable = new TextureRegion(texture);
				}
				foreach (var room in _rooms)
				{
					room.MarkColor = null;
				}
			}
		}

		public void MeasurePushRoom(Point forceVector)
		{
			var selectedRoomId = _rooms.Grid.SelectedRoomId;
			if (selectedRoomId == null || forceVector == Point.Empty)
			{
				return;
			}

			var room = _rooms.GetRoomById(selectedRoomId.Value);
			var roomsToMark = _rooms.MeasurePushRoom(room, forceVector);
			foreach (var pair in roomsToMark)
			{
				room = _rooms.GetRoomById(pair.Key);
				room.MarkColor = SKColors.Green;
			}

			_rooms.InvalidateGrid();

			Redraw();
		}

		public void PushRoom(Point forceVector)
		{
			var selectedRoomId = _rooms.Grid.SelectedRoomId;
			if (selectedRoomId == null || forceVector == Point.Empty)
			{
				return;
			}

			var room = _rooms.GetRoomById(selectedRoomId.Value);
			_rooms.PushRoom(room, forceVector);

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