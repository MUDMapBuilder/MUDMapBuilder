using AbarimMUD.Data;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;

namespace MUDMapBuilder.Editor.UI
{
	public partial class MainForm
	{
		private readonly MapViewer _mapViewer;
		private bool _dirty = true;

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

		public int Step
		{
			get => (int)_spinButtonStep.Value;
			set => _spinButtonStep.Value = value;
		}

		public int CompactRuns
		{
			get => (int)_spinButtonCompact.Value;
			set => _spinButtonCompact.Value = value;
		}

		public System.Drawing.Point ForceVector => new System.Drawing.Point((int)_spinPushForceX.Value, (int)(_spinPushForceY.Value));

		public MainForm()
		{
			BuildUI();

			_spinButtonCompact.Value = 5;

			_mapViewer = new MapViewer();
			_panelMap.Content = _mapViewer;

			_buttonStart.Click += (s, e) => _spinButtonStep.Value = 1;
			_buttonEnd.Click += (s, e) => _spinButtonStep.Value = _mapViewer.MaxSteps;
			_spinButtonStep.ValueChanged += (s, e) => Invalidate();

			_buttonZeroCompact.Click += (s, e) => _spinButtonCompact.Value = _spinButtonCompact.Minimum;
			_buttonMaximumCompact.Click += (s, e) => _spinButtonCompact.Value = _spinButtonCompact.Maximum;
			_spinButtonCompact.ValueChanged += (s, e) => Invalidate();

			_buttonMeasure.Click += (s, e) => _mapViewer.MeasurePushRoom(ForceVector);
			_buttonPush.Click += (s, e) => _mapViewer.PushRoom(ForceVector);

			_mapViewer.BrokenConnectionsChanged += (s, e) => UpdateBrokenConnections();

			UpdateBrokenConnections();
		}

		private void UpdateBrokenConnections()
		{
			if (_mapViewer.BrokenConnections != null)
			{
				_labelNonStraightConnections.Text = $"Non Straight Connections: {_mapViewer.BrokenConnections.NonStraightConnectionsCount}";
				_labelConnectionsWithObstacles.Text = $"Connections With Obstacles: {_mapViewer.BrokenConnections.ConnectionsWithObstaclesCount}";
			} else
			{
				_labelNonStraightConnections.Text = "";
				_labelConnectionsWithObstacles.Text = "";
			}
		}

		public void Invalidate()
		{
			_dirty = true;
		}

		public override void InternalRender(RenderContext context)
		{
			base.InternalRender(context);

			if (_dirty)
			{
				_mapViewer.Rebuild((int)_spinButtonStep.Value.Value, (int)_spinButtonCompact.Value);
				_dirty = false;
			}
		}
	}
}