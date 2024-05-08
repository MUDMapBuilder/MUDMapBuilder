using AbarimMUD.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.IO;
using System.Linq;

using Point = System.Drawing.Point;

namespace MUDMapBuilder.Editor.UI
{
	public class MapViewer : Image
	{
		private Area _map;
		private RoomsCollection _rooms;
		private MMBImageResult _imageResult;
		private BrokenConnectionsInfo _brokenConnections = null;

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

		public BrokenConnectionsInfo BrokenConnections
		{
			get => _brokenConnections;

			set
			{
				if (value == _brokenConnections)
				{
					return;
				}

				_brokenConnections = value;
				BrokenConnectionsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler BrokenConnectionsChanged;

		public MapViewer()
		{
			Background = new SolidBrush(Color.White);
		}

		public void Rebuild(int? maxSteps = null, int? compactRuns = null)
		{
			var rooms = (from r in _map.Rooms select new RoomWrapper(r)).ToArray();
			_rooms = MapBuilder.Build(rooms, maxSteps, compactRuns);
			BrokenConnections = _rooms.CalculateBrokenConnections();
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

			foreach (var room in _rooms)
			{
				room.Mark = false;
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
				room.Mark = true;
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
			BrokenConnections = _rooms.CalculateBrokenConnections();

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