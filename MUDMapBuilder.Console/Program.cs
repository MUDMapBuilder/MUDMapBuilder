using System;
using System.IO;

namespace MUDMapBuilder
{
	internal class Program
	{
		private static void Log(string message) => Console.WriteLine(message);

		private static void Process(string inputPath, string outputPath)
		{
			var data = File.ReadAllText(inputPath);
			var project = MMBProject.Parse(data);

			var buildResult = MapBuilder.MultiRun(project, Log);
			if (buildResult == null)
			{
				Log("Error: No rooms to process");
				return;
			}

			var addDebugInfo = project.BuildOptions != null && project.BuildOptions.AddDebugInfo;
			var pngData = buildResult.Last.BuildPng(addDebugInfo).PngData;
			File.WriteAllBytes(outputPath, pngData);

			if (buildResult.ResultType != ResultType.Success)
			{
				Log($"Error: {buildResult.ResultType}. Try raising amount of MaxSteps in the BuildOptions.");
			}
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