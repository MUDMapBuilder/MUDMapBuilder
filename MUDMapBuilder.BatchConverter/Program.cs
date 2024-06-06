using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MUDMapBuilder.BatchConverter
{
	internal class Program
	{
		private static string _outputFolder;
		private static ConcurrentDictionary<string, string> _errors = new ConcurrentDictionary<string, string>();

		private static void Log(string message) => Console.WriteLine(message);

		static void ProcessFile(string areaFile)
		{
			var areaFileName = Path.GetFileName(areaFile);
			var imageFile = Path.ChangeExtension(Path.Combine(_outputFolder, areaFileName), "png");
			if (File.Exists(imageFile))
			{
				Log($"Map for '{areaFileName}' exits already.");
				return;
			}

			Log($"Processing file {areaFileName}...");
			var project = MMBProject.Parse(File.ReadAllText(areaFile));
			var buildResult = MapBuilder.Build(project, Log);
			if (buildResult == null)
			{
				_errors[areaFile] = "Mo rooms to process.";
				return;
			}

			if (buildResult.ResultType != ResultType.Success)
			{
				_errors[areaFile] = $"{buildResult.ResultType}. Try turning off fix options(fixObstacles/fixNonStraight/fixIntersected) for this file.";
				return;
			}

			var options = project.BuildOptions;
			options.ColorizeConnectionIssues = false;

			var pngData = buildResult.Last.BuildPng(options).PngData;
			File.WriteAllBytes(imageFile, pngData);
		}

		static void Process(string folder)
		{
			var areaFiles = Directory.EnumerateFiles(folder, "*.json");

			_outputFolder = Path.Combine(folder, "png");
			if (!Directory.Exists(_outputFolder))
			{
				Directory.CreateDirectory(_outputFolder);
			}

			Parallel.ForEach(areaFiles, areaFile => ProcessFile(areaFile));

			foreach(var pair in _errors)
			{
				Log($"Error in {pair.Key}: {pair.Value}");
			}

			if (_errors.Count > 0)
			{
				Log($"Total errors count: {_errors.Count}");
			} else
			{
				Log("Success");
			}
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					Log("Usage: mmb-bc <inputFolder>");
					return;
				}

				Process(args[0]);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}
	}
}
