using Myra.Graphics2D.UI;
using Microsoft.Xna.Framework;
using Myra;
using MUDMapBuilder.Editor.UI;
using AbarimMUD.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MUDMapBuilder.Editor
{
	public class ViewerGame : Game
	{
		private const int MapId = 27;

		private readonly GraphicsDeviceManager _graphics;

		private Desktop _desktop;
		private MainForm _mainForm;
		private readonly State _state;

		public ViewerGame()
		{
			// Restore state
			_state = State.Load();

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};
			Window.AllowUserResizing = true;
			IsMouseVisible = true;

			if (_state != null)
			{
				_graphics.PreferredBackBufferWidth = _state.Size.X;
				_graphics.PreferredBackBufferHeight = _state.Size.Y;
			}
			else
			{
				_graphics.PreferredBackBufferWidth = 1280;
				_graphics.PreferredBackBufferHeight = 800;
			}
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			_desktop = new Desktop();
			_mainForm = new MainForm();
			_desktop.Root = _mainForm;

			Area map;
			using (var db = Database.CreateDataContext())
			{
				map = (from m in db.Areas.Include(m => m.Rooms).ThenInclude(r => r.Exits) where m.Id == MapId select m).First();
			}

			_mainForm.Map = map;
			if (_state != null)
			{
				_mainForm.Step = _state.Step;
				_mainForm.CompactRuns = _state.CompactRuns;
			}
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

		protected override void EndRun()
		{
			base.EndRun();

			var state = new State
			{
				Size = new Point(GraphicsDevice.PresentationParameters.BackBufferWidth,
					GraphicsDevice.PresentationParameters.BackBufferHeight),
				Step = _mainForm.Step,
				CompactRuns = _mainForm.CompactRuns,
			};

			state.Save();
		}
	}
}