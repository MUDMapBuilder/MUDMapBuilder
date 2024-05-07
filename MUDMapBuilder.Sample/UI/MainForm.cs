using AbarimMUD.Data;

namespace MUDMapBuilder.Sample.UI
{
	public partial class MainForm
	{
		private readonly MapViewer _mapViewer;

		public Area Map
		{
			get => _mapViewer.Map;
			set
			{
				_mapViewer.Map = value;
				_spinButtonStep.Minimum = 1;
				_spinButtonStep.Maximum = _mapViewer.MaxSteps;
				_spinButtonStep.Value = _mapViewer.MaxSteps;
			}
		}

		public MainForm()
		{
			BuildUI();

			_spinButtonCompact.Value = 5;

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _mapViewer.MaxSteps;
			_spinButtonStep.ValueChanged += (s, e) => Rebuild();

			_buttonZeroCompact.Click += (s, e) => _spinButtonCompact.Value = _spinButtonCompact.Minimum;
			_buttonMaximumCompact.Click += (s, e) => _spinButtonCompact.Value = _spinButtonCompact.Maximum;
			_spinButtonCompact.ValueChanged += (s, e) => Rebuild();
		}

		private void Rebuild()
		{
			_mapViewer.Rebuild((int)_spinButtonStep.Value.Value, (int)_spinButtonCompact.Value);
		}
	}
}