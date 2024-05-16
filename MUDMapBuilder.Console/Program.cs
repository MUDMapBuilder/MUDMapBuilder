using System;
using System.IO;
using System.Text;

namespace MUDMapBuilder
{
	internal class Program
	{
		private static void Log(string message) => Console.WriteLine(message);

		private static void Process(string inputPath, string outputPath)
		{
			var data = File.ReadAllText(inputPath);
			var project = MMBProject.Parse(data);

			var buildResult = MapBuilder.Build(project, Log);

			var options = project.BuildOptions;
			options.ColorizeConnectionIssues = false;

			var pngData = buildResult.Last.BuildPng(options).PngData;
			File.WriteAllBytes(outputPath, pngData);
		}

		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: mmb <input> <output>");
				Console.WriteLine("Example: mmb Midgaard.json Midgaard.png");
				return;
			}

			var inputPath = args[0];
			var outputPath = args[1];

			if (!outputPath.EndsWith(".png"))
			{
				Console.WriteLine("output name should end with .png");
				return;
			}

			try
			{
				Process(inputPath, outputPath);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}
	}
}