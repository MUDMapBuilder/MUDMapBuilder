using Myra.Graphics2D.UI;
using Microsoft.Xna.Framework;
using Myra;
using MUDMapBuilder.Editor.UI;

namespace MUDMapBuilder.Editor
{
	public class ViewerGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;

		private Desktop _desktop;
		private MainForm _mainForm;
		private readonly State _state;

		public static ViewerGame Instance { get; private set; }

		public string FilePath
		{
			get => Window.Title;
			set => Window.Title = value;
		}

		public ViewerGame()
		{
			Instance = this;

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

			if (_state != null)
			{
				_mainForm.Step = _state.Step;
				_mainForm.Straighten = _state.Straighten;
				_mainForm.Compact = _state.Compact;
				_mainForm.LoadArea(_state.EditedFile);
				_mainForm.Step = _state.Step;
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
				EditedFile = FilePath,
				Step = _mainForm.Step,
				Straighten = _mainForm.Straighten,
				Compact = _mainForm.Compact,
			};

			state.Save();
		}
	}
}