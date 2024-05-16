using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MUDMapBuilder.Editor.UI
{
	public partial class MainForm
	{
		private string _filePath;
		private bool _isDirty;
		private readonly MapViewer _mapViewer;
		private bool _suspendUi = false;

		private MMBArea Area { get; set; }

		public int Step
		{
			get => (int)_spinButtonStep.Value;
			set => _spinButtonStep.Value = value;
		}

		public string FilePath
		{
			get => _filePath;

			set
			{
				if (value == _filePath)
				{
					return;
				}

				_filePath = value;

				UpdateTitle();
				UpdateEnabled();
			}
		}

		public bool IsDirty
		{
			get
			{
				return _isDirty;
			}

			set
			{
				if (value == _isDirty)
				{
					return;
				}

				_isDirty = value;
				UpdateTitle();
			}
		}

		public MainForm()
		{
			BuildUI();

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_menuItemFileOpen.Selected += (s, e) => OnMenuFileOpenSelected();
			_menuItemFileSave.Selected += (s, e) => Save(false);
			_menuItemFileSaveAs.Selected += (s, e) => Save(true);

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _spinButtonStep.Maximum;
			_buttonToCompact.Click += (s, e) => _spinButtonStep.Value = _mapViewer.Result.StartCompactStep;
			_spinButtonStep.ValueChanged += (s, e) =>
			{
				if (_suspendUi)
				{
					return;
				}

				_mapViewer.Step = (int)_spinButtonStep.Value;
				UpdateNumbers();
			};

			_mapViewer.SelectedIndexChanged += (s, e) => UpdateEnabled();

			_checkFixObstacles.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkFixNonStraight.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkIntersected.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();

			UpdateEnabled();
		}

		private void ClearRoomsMark()
		{
			if (_mapViewer.Area == null)
			{
				return;
			}

			foreach (var room in _mapViewer.Area)
			{
				room.MarkColor = null;
				room.ForceMark = null;
			}
		}

		private void ProcessSave(string filePath)
		{
			var data = Area.ToJson();
			File.WriteAllText(filePath, data);

			FilePath = filePath;
			IsDirty = false;
		}

		private void Save(bool setFileName)
		{
			if (string.IsNullOrEmpty(FilePath) || setFileName)
			{
				var dlg = new FileDialog(FileDialogMode.SaveFile)
				{
					Filter = "*.json"
				};

				if (!string.IsNullOrEmpty(FilePath))
				{
					dlg.FilePath = FilePath;
				}

				dlg.ShowModal(Desktop);

				dlg.Closed += (s, a) =>
				{
					if (dlg.Result)
					{
						ProcessSave(dlg.FilePath);
					}
				};
			}
			else
			{
				ProcessSave(FilePath);
			}
		}

		public void OnMenuFileOpenSelected()
		{
			FileDialog dialog = new FileDialog(FileDialogMode.OpenFile)
			{
				Filter = "*.json"
			};

			if (!string.IsNullOrEmpty(FilePath))
			{
				dialog.Folder = Path.GetDirectoryName(FilePath);
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
			if (_mapViewer.Area != null)
			{
				_labelRoomsCount.Text = $"Rooms Count: {_mapViewer.Area.PositionedRoomsCount}/{_mapViewer.Area.Count}";
				_labelGridSize.Text = $"Grid Size: {_mapViewer.Area.Width}x{_mapViewer.Area.Height}";
				_labelStartCompactStep.Text = $"Start Compact Step: {_mapViewer.Result.StartCompactStep}";

				var brokenConnections = _mapViewer.Area.BrokenConnections;
				_labelIntersectedConnections.Text = $"Intersected Connections: {brokenConnections.Intersections.Count}";
				_labelNonStraightConnections.Text = $"Non Straight Connections: {brokenConnections.NonStraight.Count}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {brokenConnections.WithObstacles.Count}";
				_labelLongConnections.Text = $"Long Connections: {brokenConnections.Long.Count}";
			}
			else
			{
				_labelRoomsCount.Text = "";
				_labelGridSize.Text = "";
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

			UpdateNumbers();
		}

		private void InternalRebuild(string newFilePath = null)
		{
			Utility.QueueUIAction(() =>
			{
				_mapViewer.Result = null;
			});

			var result = MapBuilder.Build(Area, Utility.SetStatusMessage);

			Utility.QueueUIAction(() =>
			{
				_mapViewer.Result = result;
				var maxSteps = _mapViewer.Result.History.Length;
				_spinButtonStep.Maximum = maxSteps;
				_spinButtonStep.Value = maxSteps;

				if (newFilePath != null)
				{
					FilePath = newFilePath;
				}

				UpdateEnabled();
			});
		}

		private void SetDirtyAndRebuild()
		{
			if (_suspendUi)
			{
				return;
			}

			IsDirty = true;
			Rebuild();
		}

		private void Rebuild()
		{
			try
			{
				Area.BuildOptions.FixObstacles = _checkFixObstacles.IsChecked;
				Area.BuildOptions.FixNonStraight = _checkFixNonStraight.IsChecked;
				Area.BuildOptions.FixIntersected = _checkIntersected.IsChecked;

				Task.Run(() => InternalRebuild());
			}
			catch (Exception ex)
			{
				var dialog = Dialog.CreateMessageBox("Error", ex.ToString());
				dialog.ShowModal(Desktop);
			}
		}

		public void ImportArea(string path)
		{
			try
			{
				var data = File.ReadAllText(path);
				Area = MMBArea.Parse(data);

				try
				{
					_suspendUi = true;

					var options = Area.BuildOptions;
					_checkFixObstacles.IsChecked = options.FixObstacles;
					_checkFixNonStraight.IsChecked = options.FixNonStraight;
					_checkIntersected.IsChecked = options.FixIntersected;
				}
				finally
				{
					_suspendUi = false;
				}

				Task.Run(() => {
					InternalRebuild(path);
				});
			}
			catch (Exception ex)
			{
				var dialog = Dialog.CreateMessageBox("Error", ex.ToString());
				dialog.ShowModal(Desktop);
			}
		}

		private void UpdateTitle()
		{
			var title = string.IsNullOrEmpty(_filePath) ? "MUDMapBuilder.Editor" : _filePath;

			if (_isDirty)
			{
				title += " *";
			}

			EditorGame.Instance.Window.Title = title;
		}
	}
}