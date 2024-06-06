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
		private static int AreaNumericValue(MMBArea area)
		{
			if (string.IsNullOrEmpty(area.MinimumLevel))
			{
				return int.MinValue;
			}

			int minimumLevel;
			if (!int.TryParse(area.MinimumLevel, out minimumLevel))
			{
				if (area.MinimumLevel.Equals("all", StringComparison.OrdinalIgnoreCase))
				{
					return int.MaxValue / 2;
				}

				return int.MaxValue;
			}

			int maximumLevel;
			if (!int.TryParse(area.MaximumLevel, out maximumLevel))
			{
				maximumLevel = 150;
			}

			var result = maximumLevel * 1000;
			result += minimumLevel;

			return result;
		}

		static void Process(string mudName, string folder)
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

			var outputFolder = $"data/{mudName}/maps/json/";
			if (Directory.Exists(outputFolder))
			{
				Directory.Delete(outputFolder, true);
			}

			Directory.CreateDirectory(outputFolder);

			// Save all areas
			foreach (var area in areas)
			{
				var fileName = $"{area.Name}.json";
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
				File.WriteAllText(Path.Combine(outputFolder, fileName), data);
			}

			// Generate area table
			var sb = new StringBuilder();

			sb.AppendLine("---");
			sb.AppendLine("layout: page");
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine("Name|Credits|Minimum Level|Maximum Level");

			var orderedAreas = (from a in areas orderby AreaNumericValue(a) select a).ToList();
			foreach (var area in orderedAreas)
			{
				sb.AppendLine($"[{area.Name}](/data/{mudName}/maps/png/{area.Name}.png)|{area.Credits}|{area.MinimumLevel}|{area.MaximumLevel}");
			}

			File.WriteAllText($"{mudName}_Maps.markdown", sb.ToString());
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length < 2)
				{
					Console.WriteLine("Usage: mmb-import <mudName> <inputFolder>");
					Console.WriteLine("Example: mmb-import tbaMUD \"D:\\Projects\\chaos\\tbamud\\lib\\world\"");
					return;
				}

				Process(args[0], args[1]);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
