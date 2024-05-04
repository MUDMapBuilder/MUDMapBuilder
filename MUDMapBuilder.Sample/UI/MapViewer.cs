using AbarimMUD.Data;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace MUDMapBuilder.Sample.UI
{
	public class MapViewer : Grid
	{
		private Area _map;

		public Area Map
		{
			get => _map;

			set
			{
				if (value == _map)
				{
					return;
				}

				_map = value;
				Rebuild();
			}
		}

		public MapViewer()
		{
			DefaultColumnProportion = Proportion.Auto;
			DefaultRowProportion = Proportion.Auto;
			ColumnSpacing = 8;
			RowSpacing = 8;
		}

		private void Rebuild()
		{
			Widgets.Clear();

			var mapBuilder = new MapBuilder();
			var grid = mapBuilder.BuildGrid(_map);

			for (var x = 0; x < grid.Width; x++)
			{
				for (var y = 0; y < grid.Height; y++)
				{
					var room = grid[x, y];
					if (room == null)
					{
						continue;
					}

					var label = new Label
					{
						Text = room.Room.Name,
						TextColor = Color.Black,
						BorderThickness = new Thickness(2),
						Border = new SolidBrush(Color.Black),
						Padding = new Thickness(4),
					};

					Grid.SetColumn(label, x);
					Grid.SetRow(label, y);
					Widgets.Add(label);
				}
			}
		}
	}
}
