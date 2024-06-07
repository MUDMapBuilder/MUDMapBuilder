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
			var settings = new ImporterSettings(folder, SourceType.Circle);

			var files = Directory.EnumerateFiles(folder, "*.wld", SearchOption.AllDirectories);
			foreach (var path in files)
			{
				var area = Importer.ProcessFile(settings, path);
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
/*			if (Directory.Exists(outputFolder))
			{
				Directory.Delete(outputFolder, true);
			}

			Directory.CreateDirectory(outputFolder);*/

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
				var outputPath = Path.Combine(outputFolder, fileName);
				if (File.Exists(outputPath))
				{
					Console.Write($"File '{fileName}' exists already. Trying to copy the build options...");

					// Copy build options
					try
					{
						var oldData = File.ReadAllText(outputPath);
						var oldProject = MMBProject.Parse(oldData);
						oldProject.BuildOptions.CopyTo(project.BuildOptions);
					}
					catch (Exception)
					{
						Console.WriteLine("Failed to load the existing json");
					}
				}

				var data = project.ToJson();
				File.WriteAllText(outputPath, data);
			}

			// Generate area table
			var sb = new StringBuilder();

			sb.AppendLine("var data = [");
			foreach (var area in areas)
			{
				var pngLink = $"data/{mudName}/maps/png/{area.Name}.png";
				var jsonLink = $"data/{mudName}/maps/json/{area.Name}.json";
				sb.AppendLine($"[[\"{area.Name}\", \"{pngLink}\"], \"{area.Credits}\", \"{area.MinimumLevel}\", \"{area.MaximumLevel}\", \"{jsonLink}\"],");
			}
			sb.AppendLine("];");

			var page = Resources.MapsPageTemplate;
			page = page.Replace("%title", $"{mudName}'s Maps");
			page = page.Replace("%data%", sb.ToString());
			File.WriteAllText($"{mudName}_Maps.html", page);

			// Generate eqlist table
			sb.Clear();

			sb.AppendLine("var data = [");
			foreach (var area in areas)
			{
				foreach (var obj in area.Objects)
				{
					if (obj.ItemType != ItemType.Weapon && obj.ItemType != ItemType.Armor)
					{
						continue;
					}

					var flag = obj.WearFlags;
					flag &= ~WearFlags.Take;
					if (flag == 0)
					{
						continue;
					}

					sb.AppendLine($"[\"{obj.Name}\", \"{area.Name}\", \"{flag.ToString()}\", \"{obj.Level}\", \"{obj.BuildStringValue()}\", \"{obj.ExtraFlags}\", \"{obj.BuildEffectsValue()}\"],");
				}
			}
			sb.AppendLine("];");


			page = Resources.EqPageTemplate;
			page = page.Replace("%title", $"{mudName}'s Equipment");
			page = page.Replace("%data%", sb.ToString());

			File.WriteAllText($"{mudName}_Eq.html", page);
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length < 2)
				{
					Console.WriteLine("Usage: mmb-import <mudName> <inputFolder>");
					Console.WriteLine("Example: mmb-import tbaMUD \"D:\\Projects\\chaos\\tbamud\\lib\\world\\");
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
