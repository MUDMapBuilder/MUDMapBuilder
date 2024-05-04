using AbarimMUD.Data;

namespace MUDMapBuilder.Sample.UI
{
	public partial class MainForm
	{
		private readonly MapViewer _mapViewer;

		public Area Map
		{
			get => _mapViewer.Map;
			set => _mapViewer.Map = value;
		}

		public MainForm()
		{
			BuildUI();

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;
		}
	}
}