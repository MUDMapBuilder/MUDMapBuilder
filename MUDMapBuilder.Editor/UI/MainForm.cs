using MUDMapBuilder.Editor.Data;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using SkiaSharp;
using System;
using System.Drawing;
using System.IO;

namespace MUDMapBuilder.Editor.UI
{
	public partial class MainForm
	{
		private readonly MapViewer _mapViewer;
		private bool _suspendStep = false;

		public Area Area
		{
			get => _mapViewer.Area;
		}

		public int Step
		{
			get => (int)_spinButtonStep.Value;
			set => _spinButtonStep.Value = value;
		}

		public MainForm()
		{
			BuildUI();

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_menuItemImport.Selected += (s, e) => OnMenuFileImportSelected();

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _spinButtonStep.Maximum;
			_buttonToCompact.Click += (s, e) => _spinButtonStep.Value = _mapViewer.Result.StartCompactStep;
			_spinButtonStep.ValueChanged += (s, e) =>
			{
				if (_suspendStep)
				{
					return;
				}

				_mapViewer.Step = (int)_spinButtonStep.Value;
				UpdateNumbers();
			};

			_mapViewer.SelectedIndexChanged += (s, e) => UpdateEnabled();

			_buttonMeasureEast.Click += (s, e) => MeasureCompactPush(MMBDirection.East);
			_buttonMeasureWest.Click += (s, e) => MeasureCompactPush(MMBDirection.West);
			_buttonMeasureNorth.Click += (s, e) => MeasureCompactPush(MMBDirection.North);
			_buttonMeasureSouth.Click += (s, e) => MeasureCompactPush(MMBDirection.South);

			_buttonPushEast.Click += (s, e) => CompactPush(MMBDirection.East);
			_buttonPushWest.Click += (s, e) => CompactPush(MMBDirection.West);
			_buttonPushNorth.Click += (s, e) => CompactPush(MMBDirection.North);
			_buttonPushSouth.Click += (s, e) => CompactPush(MMBDirection.South);

			UpdateEnabled();
		}

		private void ClearRoomsMark()
		{
			if (_mapViewer.Rooms == null)
			{
				return;
			}

			foreach (var room in _mapViewer.Rooms)
			{
				room.MarkColor = null;
				room.ForceMark = null;
			}
		}

		private void MeasureCompactPush(MMBDirection direction)
		{
			ClearRoomsMark();

			var measure = _mapViewer.Rooms.MeasureCompactPushRoom(_mapViewer.SelectedRoomId.Value, direction);
			foreach (var r in measure.DeletedRooms)
			{
				r.MarkColor = SKColors.Red;
			}

			foreach (var m in measure.MovedRooms)
			{
				m.Room.MarkColor = SKColors.YellowGreen;
				m.Room.ForceMark = m.Delta;
			}

			_mapViewer.Redraw();
		}

		private void CompactPush(MMBDirection direction)
		{
			ClearRoomsMark();

			var measure = _mapViewer.Rooms.MeasureCompactPushRoom(_mapViewer.SelectedRoomId.Value, direction);
			if (measure.DeletedRooms.Length > 0)
			{
				foreach (var r in measure.DeletedRooms)
				{
					r.MarkColor = SKColors.Red;
				}

				var dialog = Dialog.CreateMessageBox("Error", "Such push would overlap other rooms(marked as red)");
				dialog.ShowModal(Desktop);
			}
			else
			{
				// Test push
				var vc = _mapViewer.Rooms.BrokenConnections;
				var rooms = _mapViewer.Rooms.Clone();

				foreach (var m in measure.MovedRooms)
				{
					var newPos = new Point(m.Room.Position.Value.X + m.Delta.X,
						m.Room.Position.Value.Y + m.Delta.Y);

					var roomClone = rooms.GetRoomById(m.Room.Id);
					roomClone.Position = newPos;
				}

				var vc2 = rooms.BrokenConnections;
				if (vc2.WithObstacles.Count > vc.WithObstacles.Count ||
					vc2.NonStraight.Count > vc.NonStraight.Count)
				{
					var dialog = Dialog.CreateMessageBox("Error", "Such push would break some room connections");
					dialog.ShowModal(Desktop);
				}
				else if (rooms.GridArea > _mapViewer.Rooms.GridArea)
				{
					var dialog = Dialog.CreateMessageBox("Error", "Such push would make the grid bigger");
					dialog.ShowModal(Desktop);
				}
				else if (PositionedRooms.AreEqual(rooms, _mapViewer.Rooms))
				{
					var dialog = Dialog.CreateMessageBox("Error", "Such push wouldn't change anything");
					dialog.ShowModal(Desktop);
				}
				else
				{
					// Do the move
					foreach (var m in measure.MovedRooms)
					{
						var newPos = new Point(m.Room.Position.Value.X + m.Delta.X,
							m.Room.Position.Value.Y + m.Delta.Y);

						m.Room.Position = newPos;
					}
				}
			}

			_mapViewer.Redraw();
			UpdateEnabled();
		}

		public void OnMenuFileImportSelected()
		{
			FileDialog dialog = new FileDialog(FileDialogMode.OpenFile)
			{
				Filter = "*.json"
			};

			if (!string.IsNullOrEmpty(ViewerGame.Instance.FilePath))
			{
				dialog.Folder = Path.GetDirectoryName(ViewerGame.Instance.FilePath);
			}

			dialog.Closed += (s, a) =>
			{
				if (!dialog.Result)
				{
					// "Cancel" or Escape
					return;
				}

				// "Ok" or Enter
				ImportArea(dialog.FilePath);
			};

			dialog.ShowModal(Desktop);
		}

		private void UpdateNumbers()
		{
			if (_mapViewer.Rooms != null)
			{
				_labelRoomsCount.Text = $"Rooms Count: {_mapViewer.Rooms.PositionedRoomsCount}/{_mapViewer.Area.Rooms.Count}";
				_labelGridSize.Text = $"Grid Size: {_mapViewer.Rooms.Width}x{_mapViewer.Rooms.Height}";
				_labelGridArea.Text = $"Grid Area: {_mapViewer.Rooms.GridArea}";
				_labelStartCompactStep.Text = $"Start Compact Step: {_mapViewer.Result.StartCompactStep}";

				var brokenConnections = _mapViewer.Rooms.BrokenConnections;
				_labelIntersectedConnections.Text = $"Intersected Connections: {brokenConnections.Intersections.Count}";
				_labelNonStraightConnections.Text = $"Non Straight Connections: {brokenConnections.NonStraight.Count}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {brokenConnections.WithObstacles.Count}";
				_labelLongConnections.Text = $"Long Connections: {brokenConnections.Long.Count}";
			}
			else
			{
				_labelRoomsCount.Text = "";
				_labelGridSize.Text = "";
				_labelGridArea.Text = "";
				_labelStartCompactStep.Text = "";
				_labelIntersectedConnections.Text = "";
				_labelNonStraightConnections.Text = "";
				_labelConnectionsWithObstacles.Text = "";
				_labelLongConnections.Text = "";
			}
		}

		private void UpdateEnabled()
		{
			ClearRoomsMark();

			var enabled = Area != null;

			_menuItemFileSave.Enabled = enabled;
			_menuItemFileSaveAs.Enabled = enabled;
			_buttonStart.Enabled = enabled;
			_buttonToCompact.Enabled = enabled;
			_buttonEnd.Enabled = enabled;
			_spinButtonStep.Enabled = enabled;

			enabled = enabled && _mapViewer.SelectedRoomId != null;
			_buttonMeasureEast.Enabled = enabled;
			_buttonMeasureWest.Enabled = enabled;
			_buttonMeasureNorth.Enabled = enabled;
			_buttonMeasureSouth.Enabled = enabled;
			_buttonPushEast.Enabled = enabled;
			_buttonPushWest.Enabled = enabled;
			_buttonPushNorth.Enabled = enabled;
			_buttonPushSouth.Enabled = enabled;

			UpdateNumbers();
		}

		public void ImportArea(string path)
		{
			try
			{
				var data = File.ReadAllText(path);
				_mapViewer.Area = Area.Parse(data);
				var maxSteps = _mapViewer.Result.History.Length;

				_spinButtonStep.Maximum = maxSteps;
				try
				{
					_suspendStep = true;
					_spinButtonStep.Value = maxSteps;
				}
				finally
				{
					_suspendStep = false;
				}

				ViewerGame.Instance.FilePath = path;
				UpdateEnabled();
			}
			catch (Exception ex)
			{
				var dialog = Dialog.CreateMessageBox("Error", ex.ToString());
				dialog.ShowModal(Desktop);
			}
		}
	}
}