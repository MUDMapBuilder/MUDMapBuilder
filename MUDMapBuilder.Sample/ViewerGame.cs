using Myra.Graphics2D.UI;
using Microsoft.Xna.Framework;
using Myra;
using MUDMapBuilder.Sample.UI;
using AbarimMUD.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Myra.Graphics2D.Brushes;

namespace MUDMapBuilder.Sample
{
	public class ViewerGame : Game
	{
		private const int MapId = 27;

		private readonly GraphicsDeviceManager _graphics;

		private Desktop _desktop;
		private MainForm _mainForm;

		public ViewerGame()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};
			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			_desktop = new Desktop();
			_desktop.Background = new SolidBrush(Color.White);

			_mainForm = new MainForm();
			_desktop.Root = _mainForm;

			Area map;
			using (var db = Database.CreateDataContext())
			{
				map = (from m in db.Areas.Include(m => m.Rooms).ThenInclude(r => r.Exits) where m.Id == MapId select m).First();
			}

			_mainForm.Map = map;
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);
			_desktop.Render();
		}
	}
}