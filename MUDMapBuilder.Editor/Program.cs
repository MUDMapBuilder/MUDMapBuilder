﻿namespace MUDMapBuilder.Editor
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var game = new ViewerGame())
				game.Run();
		}
	}
}
