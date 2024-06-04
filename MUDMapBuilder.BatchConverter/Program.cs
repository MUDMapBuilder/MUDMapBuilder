using System.IO;
using System;
using System.Linq;

namespace MUDMapBuilder.BatchConverter
{
	internal class Program
	{
		private static void Log(string message) => Console.WriteLine(message);

		static void Process(string folder)
		{
			var areaFiles = Directory.EnumerateFiles(folder, "*.json");

			var outputFolder = Path.Combine(folder, "png");
			if (!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}

			foreach (var areaFile in areaFiles)
			{
				var areaFileName = Path.GetFileName(areaFile);
				var imageFile = Path.ChangeExtension(Path.Combine(outputFolder, areaFileName), "png");
				if (File.Exists(imageFile))
				{
					Console.WriteLine($"Map for '{areaFileName}' exits already.");
					continue;
				}

				Console.WriteLine($"Processing file {areaFileName}...");
				var project = MMBProject.Parse(File.ReadAllText(areaFile));
				var buildResult = MapBuilder.Build(project, Log);
				if (buildResult == null)
				{
					Log($"Warning: no rooms to process. Skipping.");
					continue;
				}

				if (buildResult.History.Length >= project.BuildOptions.MaxSteps)
				{
					throw new Exception($"WARNING: The process wasn't completed for {areaFileName}. Try turning off fix options(fixObstacles/fixNonStraight/fixIntersected).");
				}

				var options = project.BuildOptions;
				options.ColorizeConnectionIssues = false;

				var pngData = buildResult.Last.BuildPng(options).PngData;
				File.WriteAllBytes(imageFile, pngData);
			}
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					Console.WriteLine("Usage: mmb-bc <inputFolder>");
					return;
				}

				Process(args[0]);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
