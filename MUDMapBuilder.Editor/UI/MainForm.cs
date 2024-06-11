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

		private MMBProject Project { get; set; }
		private MapBuilderResult Result { get; set; }

		private MMBArea Area
		{
			get
			{
				if (Result == null)
				{
					return null;
				}

				var step = Math.Min(Step, Result.History.Length - 1);
				return Result.History[step];
			}
		}

		private BuildOptions Options => Project.BuildOptions;

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
			_buttonToCompact.Click += (s, e) => _spinButtonStep.Value = Result.StartCompactStep;
			_spinButtonStep.ValueChanged += (s, e) =>
			{
				if (_suspendUi)
				{
					return;
				}

				_mapViewer.Redraw(Area, Options, _checkColorizeConnectionIssues.IsChecked);
				UpdateNumbers();
			};

			_mapViewer.SelectedIndexChanged += (s, e) => UpdateEnabled();

			_checkRemoveSolitaryRooms.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkRemoveRoomsWithSingleOutsideExit.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkFixObstacles.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkFixNonStraight.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkFixIntersected.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkCompactMap.IsCheckedChanged += (s, e) => SetDirtyAndRebuild();
			_checkAddDebugInfo.IsCheckedChanged += (s, e) => SetDirtyAndRedraw();
			_checkColorizeConnectionIssues.IsCheckedChanged += (s, e) => SetDirtyAndRedraw();

			UpdateEnabled();
		}

		private void ProcessSave(string filePath)
		{
			var data = Project.ToJson();
			File.WriteAllText(filePath, data);

			FilePath = filePath;
			IsDirty = false;
		}

		public void Save(bool setFileName)
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
			var area = Area;
			if (area != null)
			{
				_labelRoomsCount.Text = $"Rooms Count: {area.PositionedRoomsCount}/{area.Count}";
				_labelGridSize.Text = $"Grid Size: {area.Width}x{area.Height}";
				_labelStartCompactStep.Text = $"Start Compact Step: {Result.StartCompactStep}";

				var brokenConnections = area.BrokenConnections;
				_labelIntersectedConnections.Text = $"Intersected Connections: {brokenConnections.Intersections.Count}";
				_labelNonStraightConnections.Text = $"Non Straight Connections: {brokenConnections.NonStraight.Count}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {brokenConnections.WithObstacles.Count}";
				_labelLongConnections.Text = $"Long Connections: {brokenConnections.Long.Count}";
				_labelStatus.Text = Area.LogMessage;
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
			var enabled = Area != null;

			if (enabled)
			{
				Area.ClearMarks();
			}

			_menuItemFileSave.Enabled = enabled;
			_menuItemFileSaveAs.Enabled = enabled;
			_buttonStart.Enabled = enabled;
			_buttonToCompact.Enabled = enabled;
			_buttonEnd.Enabled = enabled;
			_spinButtonStep.Enabled = enabled;

			UpdateNumbers();
		}

		private void InternalRebuild(string newFilePath = null, bool clearDirty = false)
		{
			Utility.QueueUIAction(() =>
			{
				_mapViewer.Redraw(null, null, _checkColorizeConnectionIssues.IsChecked);
			});

			var result = MapBuilder.SingleRun(Project, Utility.SetStatusMessage, 
				_checkFixObstacles.IsChecked, _checkFixNonStraight.IsChecked,
				_checkFixIntersected.IsChecked, _checkCompactMap.IsChecked);

			Utility.QueueUIAction(() =>
			{
				Result = result;
				_mapViewer.Redraw(Area, Options, _checkColorizeConnectionIssues.IsChecked);
				var maxSteps = Result.History.Length;
				_spinButtonStep.Maximum = maxSteps;
				_spinButtonStep.Value = maxSteps;

				if (newFilePath != null)
				{
					FilePath = newFilePath;
					if (clearDirty)
					{
						IsDirty = false;
					}
				}

				UpdateEnabled();
			});
		}

		private void SetBuildOptionsFromUI()
		{
			Project.BuildOptions.RemoveSolitaryRooms = _checkRemoveSolitaryRooms.IsChecked;
			Project.BuildOptions.RemoveRoomsWithSingleOutsideExit = _checkRemoveRoomsWithSingleOutsideExit.IsChecked;
			Project.BuildOptions.AddDebugInfo = _checkAddDebugInfo.IsChecked;
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

		private void SetDirtyAndRedraw()
		{
			if (_suspendUi)
			{
				return;
			}

			IsDirty = true;
			SetBuildOptionsFromUI();
			_mapViewer.Redraw(Area, Options, _checkColorizeConnectionIssues.IsChecked);
		}

		private void Rebuild()
		{
			try
			{
				SetBuildOptionsFromUI();
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
				Project = MMBProject.Parse(data);

				if (Project.Area == null || Project.Area.Rooms == null || Project.Area.Rooms.Length == 0)
				{
					var dialog = Dialog.CreateMessageBox("Error", $"Area '{Project.Area.Name}' has no rooms");
					dialog.ShowModal(Desktop);
					return;
				}

				try
				{
					_suspendUi = true;

					var options = Project.BuildOptions;
					_checkRemoveSolitaryRooms.IsChecked = options.RemoveSolitaryRooms;
					_checkRemoveRoomsWithSingleOutsideExit.IsChecked = options.RemoveRoomsWithSingleOutsideExit;
					_checkAddDebugInfo.IsChecked = options.AddDebugInfo;
				}
				finally
				{
					_suspendUi = false;
				}

				Task.Run(() =>
				{
					InternalRebuild(path, true);
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