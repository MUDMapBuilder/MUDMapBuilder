using System.IO;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

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

			var buildResult = MapBuilder.MultiRun(project, Log);
			if (buildResult == null)
			{
				_errors[areaFile] = "No rooms to process.";
				return;
			}

			if (buildResult.ResultType != ResultType.Success)
			{
				_errors[areaFile] = $"{buildResult.ResultType}. Try raising amount of MaxSteps in the BuildOptions.";
				return;
			}

			var pngData = buildResult.Last.BuildPng(project.BuildOptions, false).PngData;
			File.WriteAllBytes(imageFile, pngData);
		}

		static void Process(string folder, string outputFolder)
		{
			var areaFiles = Directory.EnumerateFiles(folder, "*.json");

			_outputFolder = outputFolder;
			if (!Directory.Exists(_outputFolder))
			{
				Directory.CreateDirectory(_outputFolder);
			}

			Parallel.ForEach(areaFiles, areaFile => ProcessFile(areaFile));

			var orderedErrors = (from e in _errors orderby e.Key select e);
			foreach (var pair in orderedErrors)
			{
				Log($"Error in {pair.Key}: {pair.Value}");
			}

			if (_errors.Count > 0)
			{
				Log($"Total errors count: {_errors.Count}");
			}
			else
			{
				Log("Success");
			}
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length < 2)
				{
					Log("Usage: mmb-bc <inputFolder> <outputFolder>");
					return;
				}

				Process(args[0], args[1]);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}
	}
}
