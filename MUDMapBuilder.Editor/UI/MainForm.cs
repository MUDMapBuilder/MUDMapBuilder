using MUDMapBuilder.Editor.Data;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MUDMapBuilder.Editor.UI
{
	public partial class MainForm
	{
		private readonly MapViewer _mapViewer;
		private bool _suspendStep = false;

		private Area Area { get; set; }

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

			_checkFixObstacles.IsCheckedChanged += (s, e) => Rebuild();
			_checkFixNonStraight.IsCheckedChanged += (s, e) => Rebuild();
			_checkIntersected.IsCheckedChanged += (s, e) => Rebuild();

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
				_labelRoomsCount.Text = $"Rooms Count: {_mapViewer.Rooms.PositionedRoomsCount}/{_mapViewer.Rooms.Count}";
				_labelGridSize.Text = $"Grid Size: {_mapViewer.Rooms.Width}x{_mapViewer.Rooms.Height}";
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

		private void QueueUIAction(Action action)
		{
			ViewerGame.Instance.QueueUIAction(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					var dialog = Dialog.CreateMessageBox("Error", ex.ToString());
					dialog.ShowModal(Desktop);
				}
			});
		}

		private void Log(string message)
		{
			QueueUIAction(() => _labelStatus.Text = message);
		}

		private BuildOptions CreateBuildOptions()
		{
			var result = new BuildOptions
			{
				Log = Log,
				FixObstacles = _checkFixObstacles.IsChecked,
				FixNonStraight = _checkFixNonStraight.IsChecked,
				FixIntersected = _checkIntersected.IsChecked,
			};

			return result;
		}

		private void InternalRebuild(string newTitle = null)
		{
			var options = CreateBuildOptions();
			var result = MapBuilder.Build(Area.Rooms.ToArray(), options);

			QueueUIAction(() =>
			{
				_mapViewer.Result = result;
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

				if (newTitle != null)
				{
					ViewerGame.Instance.FilePath = newTitle;
				}
				UpdateEnabled();
			});
		}

		private void Rebuild()
		{
			try
			{
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
				Task.Run(() =>
				{
					var data = File.ReadAllText(path);
					Area = Area.Parse(data);

					InternalRebuild(path);
				});
			}
			catch (Exception ex)
			{
				var dialog = Dialog.CreateMessageBox("Error", ex.ToString());
				dialog.ShowModal(Desktop);
			}
		}
	}
}