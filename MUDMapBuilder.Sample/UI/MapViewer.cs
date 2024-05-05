using AbarimMUD.Data;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System.IO;

namespace MUDMapBuilder.Sample.UI
{
	public class MapViewer : Image
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

		public int MaxSteps { get; private set; }

		public MapViewer()
		{
		}

		public void Rebuild(int? maxSteps = null)
		{
			var mapBuilder = new MapBuilder();
			var grid = mapBuilder.BuildGrid(_map, maxSteps);
			if (maxSteps == null)
			{
				MaxSteps = grid.Steps;
			}

			var png = grid.BuildPng();

			using (var ms = new MemoryStream(png))
			{
				var texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, ms);
				Renderable = new TextureRegion(texture);
			}
		}
	}
}
