using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MUDMapBuilder.Editor.Data;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.IO;

using Point = System.Drawing.Point;

namespace MUDMapBuilder.Editor.UI
{
	public class MapViewer : Image
	{
		private Area _area;
		private RoomsCollection _rooms;
		private MMBImageResult _imageResult;
		private int _maxSteps = 0, _maxStraightenSteps = 0, _maxCompactSteps = 0;
		private BrokenConnectionsInfo _brokenConnections = null;

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

				// Set max steps
				var roomsArray = _area.Rooms.ToArray();
				var rooms = MapBuilder.Build(roomsArray, new BuildOptions
				{
					StraightenUsage = AlgorithmUsage.DoNotUse,
					CompactUsage = AlgorithmUsage.DoNotUse,
				});
				MaxSteps = rooms.MaxRunSteps;
			}
		}

		public int MaxSteps
		{
			get => _maxSteps;

			private set
			{
				if (value == _maxSteps)
				{
					return;
				}

				_maxSteps = value;
				MaxStepsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public int MaxStraightenSteps
		{
			get => _maxStraightenSteps;

			private set
			{
				if (value == _maxStraightenSteps)
				{
					return;
				}

				_maxStraightenSteps = value;
				MaxStraightenStepsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public int MaxCompactSteps
		{
			get => _maxCompactSteps;

			private set
			{
				if (value == _maxCompactSteps)
				{
					return;
				}

				_maxCompactSteps = value;
				MaxCompactStepsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

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

		public event EventHandler MaxStepsChanged;
		public event EventHandler MaxStraightenStepsChanged;
		public event EventHandler MaxCompactStepsChanged;
		public event EventHandler BrokenConnectionsChanged;

		public MapViewer()
		{
			Background = new SolidBrush(Color.White);
		}

		public void Rebuild(BuildOptions options)
		{
			_rooms = null;
			_imageResult = null;
			BrokenConnections = new BrokenConnectionsInfo(0, 0);

			if (_area != null)
			{
				_rooms = MapBuilder.Build(_area.Rooms.ToArray(), options);
				MaxStraightenSteps = _rooms.MaxStraightenSteps;
				MaxCompactSteps = _rooms.MaxCompactSteps;
				BrokenConnections = _rooms.CalculateBrokenConnections();
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
					room.Mark = false;
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