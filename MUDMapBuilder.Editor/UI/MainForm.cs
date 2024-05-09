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

		public Area Area
		{
			get => _mapViewer.Area;
			set
			{
				_mapViewer.Area = value;
				_spinButtonStep.Minimum = 1;
				_spinButtonStep.Maximum = _mapViewer.MaxSteps;
				_spinButtonStep.Value = _mapViewer.MaxSteps;
			}
		}

		public int Step
		{
			get => (int)_spinButtonStep.Value;
			set => _spinButtonStep.Value = value;
		}

		public bool Straighten
		{
			get => _checkButtonStraighten.IsChecked;
			set => _checkButtonStraighten.IsChecked = value;
		}

		public bool Compact
		{
			get => _checkButtonCompact.IsChecked;
			set => _checkButtonCompact.IsChecked = value;
		}

		public Point ForceVector => new Point((int)_spinPushForceX.Value, (int)(_spinPushForceY.Value));

		public MainForm()
		{
			BuildUI();

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_buttonOpen.Click += (s, e) =>
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
					LoadArea(dialog.FilePath, true);
				};

				dialog.ShowModal(Desktop);
			};

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _mapViewer.MaxSteps;
			_spinButtonStep.ValueChanged += (s, e) => RebuildMap();
			_checkButtonStraighten.IsCheckedChanged += (s, e) => RebuildMap();
			_checkButtonCompact.IsCheckedChanged += (s, e) => RebuildMap();

			_buttonMeasure.Click += (s, e) => _mapViewer.MeasurePushRoom(ForceVector);
			_buttonPush.Click += (s, e) => _mapViewer.PushRoom(ForceVector);

			_mapViewer.BrokenConnectionsChanged += (s, e) => UpdateBrokenConnections();

			UpdateEnabled();
		}

		private void RebuildMap()
		{
			var options = new BuildOptions
			{
				Steps = (int)_spinButtonStep.Value,
				Straighten = _checkButtonStraighten.IsChecked,
				Compact = _checkButtonCompact.IsChecked,
			};

			_mapViewer.Rebuild(options);
		}

		private void UpdateBrokenConnections()
		{
			if (_mapViewer.BrokenConnections != null)
			{
				_labelNonStraightConnections.Text = $"Non Straight Connections: {_mapViewer.BrokenConnections.NonStraightConnectionsCount}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {_mapViewer.BrokenConnections.ConnectionsWithObstaclesCount}";
			}
			else
			{
				_labelNonStraightConnections.Text = "";
				_labelConnectionsWithObstacles.Text = "";
			}
		}

		private void UpdateEnabled()
		{
			var enabled = Area != null;

			_buttonSave.Enabled = enabled;
			_buttonStart.Enabled = enabled;
			_buttonEnd.Enabled = enabled;
			_spinButtonStep.Enabled = enabled;
			_spinPushForceX.Enabled = enabled;
			_spinPushForceY.Enabled = enabled;
			_buttonMeasure.Enabled = enabled;
			_buttonPush.Enabled = enabled;
			UpdateBrokenConnections();
		}

		public void LoadArea(string path, bool setStepsToMax = false)
		{
			try
			{
				var data = File.ReadAllText(path);
				_mapViewer.Area = Area.Parse(data);

				if (setStepsToMax)
				{
					_spinButtonStep.Value = _mapViewer.MaxSteps;
				}
				else
				{
					RebuildMap();
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