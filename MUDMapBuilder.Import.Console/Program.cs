using MUDMapBuilder.Import.Diku;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MUDMapBuilder.Import
{
	internal class Program
	{
		private const string OutputFolder = "output";

		static void Process(string folder)
		{
			var areas = new List<MMBArea>();
			/*			var folder = @"D:\Projects\chaos\Crimson-2-MUD\prod\lib";

						var zonesTable = File.ReadAllText(Path.Combine(folder, "zones.tbl"));
						var lines = zonesTable.Split('\n');
						foreach (var line in lines)
						{
							var s = line.Trim();
							if (s == "$~")
							{
								break;
							}

							var parts = s.Split(' ');

							var path = Path.Combine(folder, "areas");
							path = Path.Combine(path, parts[0]);
							path = Path.ChangeExtension(path, "wld");

							var area = Importer.ProcessFile(path);
							areas.Add(area);

						}*/

			var files = Directory.EnumerateFiles(folder, "*.wld", SearchOption.AllDirectories);
			foreach (var path in files)
			{
				var area = Importer.ProcessFile(path);
				areas.Add(area);
			}

			// Build complete dictionary of rooms
			var allRooms = new Dictionary<int, MMBRoom>();
			foreach (var area in areas)
			{
				foreach (var room in area.Rooms)
				{
					if (allRooms.ContainsKey(room.Id))
					{
						throw new Exception($"Dublicate room id: {room.Id}");
					}

					var areaExit = room.Clone();
					areaExit.Name = $"To {area.Name}";
					areaExit.IsExitToOtherArea = true;

					allRooms[room.Id] = areaExit;
				}
			}

			// Now add areas exits
			foreach (var area in areas)
			{
				var areaExits = new Dictionary<int, MMBRoom>();
				foreach (var room in area.Rooms)
				{
					foreach (var exit in room.Connections)
					{
						MMBRoom inAreaRoom;
						inAreaRoom = (from r in area.Rooms where r.Id == exit.Value.RoomId select r).FirstOrDefault();
						if (inAreaRoom != null)
						{
							continue;
						}

						areaExits[exit.Value.RoomId] = allRooms[exit.Value.RoomId];
					}
				}

				foreach (var pair in areaExits)
				{
					area.Add(pair.Value);
				}
			}

			if (Directory.Exists(OutputFolder))
			{
				Directory.Delete(OutputFolder, true);

			}

			Directory.CreateDirectory(OutputFolder);

			// Save all areas and generate conversion script
			var sb = new StringBuilder();
			foreach (var area in areas)
			{
				var fileName = $"output/{area.Name}.json";

				if (fileName.StartsWith("Adria"))
				{
					continue;
				}

				Console.WriteLine($"Saving {fileName}...");

				var project = new MMBProject(area, new BuildOptions());

				/*				// Copy build options
								var oldData = File.ReadAllText(Path.Combine(@"D:\Temp\MUDMapBuilder.0.1.0\circle_Areas", fileName));
								var oldProject = MMBProject.Parse(oldData);

								oldProject.BuildOptions.CopyTo(project.BuildOptions);*/

				var data = project.ToJson();
				File.WriteAllText(fileName, data);

				var pngFileName = Path.ChangeExtension(fileName, "png");
				sb.AppendLine($"mmb \"circle_Areas\\{fileName}\" \"circle_Maps\\{pngFileName}\"");
			}

			File.WriteAllText("convertTba.bat", sb.ToString());
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length < 1)
				{
					Console.WriteLine("Usage: mmb-import <inputFolder>");
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
