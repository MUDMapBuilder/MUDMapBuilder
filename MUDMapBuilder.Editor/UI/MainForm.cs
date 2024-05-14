using MUDMapBuilder.Editor.Data;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
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

		public Point ForceVector => new Point((int)_spinPushForceX.Value, (int)(_spinPushForceY.Value));

		public MainForm()
		{
			BuildUI();

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_menuItemImport.Selected += (s, e) => OnMenuFileImportSelected();

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _spinButtonStep.Maximum;
			_spinButtonStep.ValueChanged += (s, e) =>
			{
				if (_suspendStep)
				{
					return;
				}

				_mapViewer.Step = (int)_spinButtonStep.Value;
			};

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
				_labelRoomsCount.Text = $"Rooms Count: {_mapViewer.Rooms.Count}/{_mapViewer.Area.Rooms.Count}";
				_labelGridSize.Text = $"Grid Size: {_mapViewer.Rooms.Width}x{_mapViewer.Rooms.Height}";

				var brokenConnections = _mapViewer.Rooms.BrokenConnections;
				_labelNonStraightConnections.Text = $"Non Straight Connections: {brokenConnections.NonStraight.Count}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {brokenConnections.WithObstacles.Count}";
				_labelLongConnections.Text = $"Long Connections: {brokenConnections.Long.Count}";
			}
			else
			{
				_labelRoomsCount.Text = "";
				_labelGridSize.Text = "";
				_labelNonStraightConnections.Text = "";
				_labelConnectionsWithObstacles.Text = "";
				_labelLongConnections.Text = "";
			}
		}

		private void UpdateEnabled()
		{
			var enabled = Area != null;

			_menuItemFileSave.Enabled = enabled;
			_menuItemFileSaveAs.Enabled = enabled;
			_buttonStart.Enabled = enabled;
			_buttonEnd.Enabled = enabled;
			_spinButtonStep.Enabled = enabled;
			_spinPushForceX.Enabled = enabled;
			_spinPushForceY.Enabled = enabled;
			_buttonMeasure.Enabled = enabled;
			_buttonPush.Enabled = enabled;

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