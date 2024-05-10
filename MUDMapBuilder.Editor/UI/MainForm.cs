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

		public AlgorithmUsage StraightenUsage
		{
			get => (AlgorithmUsage)_comboStraighten.SelectedIndex.Value;
			set => _comboStraighten.SelectedIndex = (int)value;
		}

		public int StraightenSteps
		{
			get => (int)_spinButtonStraightenSteps.Value;
			set => _spinButtonStraightenSteps.Value = value;
		}

		public AlgorithmUsage CompactUsage
		{
			get => (AlgorithmUsage)_comboCompact.SelectedIndex.Value;
			set => _comboCompact.SelectedIndex = (int)value;
		}

		public int CompactSteps
		{
			get => (int)_spinButtonCompactSteps.Value;
			set => _spinButtonCompactSteps.Value = value;
		}

		public Point ForceVector => new Point((int)_spinPushForceX.Value, (int)(_spinPushForceY.Value));

		public MainForm()
		{
			BuildUI();

			_comboStraighten.SelectedIndex = 1;
			_comboCompact.SelectedIndex = 1;

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
			_comboStraighten.SelectedIndexChanged += (s, e) =>
			{
				if (_comboStraighten.SelectedIndex != 2)
				{
					_spinButtonStraightenSteps.Value = 0;
				}
				UpdateEnabled();
				RebuildMap();
			};
			_spinButtonStraightenSteps.ValueChanged += (s, e) => RebuildMap();

			_comboCompact.SelectedIndexChanged += (s, e) =>
			{
				if (_comboCompact.SelectedIndex != 2)
				{
					_spinButtonCompactSteps.Value = 0;
				}
				UpdateEnabled();
				RebuildMap();
			};
			_spinButtonCompactSteps.ValueChanged += (s, e) => RebuildMap();

			_buttonMeasure.Click += (s, e) => _mapViewer.MeasurePushRoom(ForceVector);
			_buttonPush.Click += (s, e) => _mapViewer.PushRoom(ForceVector);

			_mapViewer.BrokenConnectionsChanged += (s, e) => UpdateBrokenConnections();
			_mapViewer.MaxStraightenStepsChanged += (s, e) =>
			{
				_labelMaxStraightenSteps.Text = $"Max Straighten Steps: {_mapViewer.MaxStraightenSteps}";
			};
			_mapViewer.MaxCompactStepsChanged += (s, e) =>
			{
				_labelMaxCompactSteps.Text = $"Max Compact Steps: {_mapViewer.MaxCompactSteps}";
			};

			UpdateEnabled();
		}

		private void RebuildMap()
		{
			var options = new BuildOptions
			{
				Steps = (int)_spinButtonStep.Value,
				StraightenUsage = StraightenUsage,
				StraightenSteps = StraightenSteps,
				CompactUsage = CompactUsage,
				CompactSteps = CompactSteps,
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
			_spinButtonStraightenSteps.Enabled = _comboStraighten.SelectedIndex == 2;
			_spinButtonCompactSteps.Enabled = _comboCompact.SelectedIndex == 2;

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