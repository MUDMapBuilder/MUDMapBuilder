namespace MUDMapBuilder.Editor
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var game = new EditorGame())
				game.Run();
		}
	}
}
