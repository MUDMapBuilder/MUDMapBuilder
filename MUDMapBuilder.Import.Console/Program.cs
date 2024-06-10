using AbarimMUD.Import.Envy;
using DikuLoad.Data;
using DikuLoad.Import;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MUDMapBuilder.Import
{
	internal class Program
	{
		static void Process(string mudName, string folder, string outputFolder)
		{
			/*			var folder = @"D:\Projects\chaos\Crimson-2-MUD\prod\lib";

						var zonesTable = File.ReadAllText(Path.Combine(folder, "zones.tbl"));
						var lines = zonesTable.Split(\"\n\");
						foreach (var line in lines)
						{
							var s = line.Trim();
							if (s == "$~")
							{
								break;
							}

							var parts = s.Split(\" \");

							var path = Path.Combine(folder, "areas");
							path = Path.Combine(path, parts[0]);
							path = Path.ChangeExtension(path, "wld");

							var area = Importer.ProcessFile(path);
							areas.Add(area);

						}*/

			BaseImporter importer;

			SourceType? sourceType = null;
			if (mudName.Contains("tba", StringComparison.OrdinalIgnoreCase) ||
				mudName.Contains("circle", StringComparison.OrdinalIgnoreCase))
			{
				sourceType = SourceType.Circle;
			}
			else if (mudName.Contains("envy", StringComparison.OrdinalIgnoreCase))
			{
				sourceType = SourceType.Envy;
			}
			else if (mudName.Contains("ROM", StringComparison.OrdinalIgnoreCase))
			{
				sourceType = SourceType.ROM;
			}

			if (sourceType != null)
			{
				var settings = new ImporterSettings(folder, sourceType.Value);
				importer = new Importer(settings);
			}
			else
			{
				importer = new DikuLoad.Import.CSL.Importer(folder);
			}

			importer.Process();

			// Convert DikuLoad areas to MMB Areas
			var areas = new List<MMBArea>();
			foreach (var dikuArea in importer.Areas)
			{
				if (dikuArea.Rooms == null || dikuArea.Rooms.Count == 0)
				{
					Console.WriteLine($"Warning: Area '{dikuArea.Name} has no rooms. Skipping.");
					continue;
				}

				areas.Add(dikuArea.ToMMBArea());
			}

			// Build complete dictionary of rooms
			var allRooms = new Dictionary<int, MMBRoom>();
			foreach (var area in areas)
			{
				foreach (var room in area.Rooms)
				{
					if (allRooms.ContainsKey(room.Id))
					{
						throw new Exception($"Dublicate room id. New room: {room}. Old room: {allRooms[room.Id]}");
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

			if (!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}

			// Save all areas
			var outputAreasCount = 0;
			foreach (var area in areas)
			{
				var fileName = $"{area.Name}.json";
				Console.WriteLine($"Saving {fileName}...");

				var project = new MMBProject(area, new BuildOptions());
				var outputPath = Path.Combine(outputFolder, fileName);
				if (File.Exists(outputPath))
				{
					Console.WriteLine($"File '{fileName}' exists already. Trying to copy the build options...");

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

				++outputAreasCount;
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
			page = page.Replace("%title%", $"{mudName}'s Maps");
			page = page.Replace("%data%", sb.ToString());
			File.WriteAllText($"{mudName}_Maps.html", page);

			// Generate eqlist table
			sb.Clear();

			sb.AppendLine("var data = [");

			var outputItemsCount = 0;
			foreach (var area in importer.Areas)
			{
				foreach (var obj in area.Objects)
				{
					if (obj.ItemType != ItemType.Weapon && obj.ItemType != ItemType.Armor)
					{
						continue;
					}

					var resets = area.RelatedResets(obj.VNum).ToArray();
					if (resets.Length == 0)
					{
						continue;
					}

					var wearFlags = obj.WearFlags;
					wearFlags &= ~ItemWearFlags.Take;
					if (wearFlags == 0)
					{
						continue;
					}

					var lines = new HashSet<string>();
					foreach (var r in resets)
					{
						switch (r.ResetType)
						{
							case AreaResetType.Item:
								// Load object in room
								{
									var room = importer.GetRoomByVnum(r.Value4);
									if (room == null)
									{
										Console.WriteLine($"WARNING: Can't find reset room {r.Value4}");
										continue;
									}

									lines.Add($"Room '{room}'");
								}

								break;
							case AreaResetType.Put:
								{
									var container = importer.GetObjectByVnum(r.Value4);
									if (container == null)
									{
										Console.WriteLine($"WARNING: Can't find container {r.Value4}");
										continue;
									}

									lines.Add($"Container '{container}'");
								}
								break;
							case AreaResetType.Give:
								{
									var mobile = importer.GetMobileByVnum(r.MobileVNum);
									if (mobile == null)
									{
										Console.WriteLine($"WARNING: Can't find mobile {r.MobileVNum}");
										continue;
									}

									lines.Add($"{mobile}");
								}
								break;
							case AreaResetType.Equip:
								{
									var mobile = importer.GetMobileByVnum(r.MobileVNum);
									if (mobile == null)
									{
										Console.WriteLine($"WARNING: Can't find mobile {r.MobileVNum}");
										continue;
									}

									lines.Add($"{mobile}");
								}
								break;
						}
					}

					var name = obj.ShortDescription.Replace("\"", "");
					var locationsStr = string.Join("<br>", lines);

					var pngLink = $"data/{mudName}/maps/png/{area.Name}.png";
					sb.AppendLine($"[\"{name}\", [\"{area.Name}\", \"{pngLink}\"], \"{locationsStr}\", \"{obj.Level}\", \"{wearFlags.BuildFlagsValue()}\", \"{obj.BuildStringValue()}\", \"{obj.ExtraFlags.BuildFlagsValue()}\", \"{obj.BuildEffectsValue()}\"],");
					++outputItemsCount;
				}
			}
			sb.AppendLine("];");


			page = Resources.EqPageTemplate;
			page = page.Replace("%title%", $"{mudName}'s Equipment");
			page = page.Replace("%data%", sb.ToString());

			File.WriteAllText($"{mudName}_Eq.html", page);

			Console.WriteLine($"Wrote {outputAreasCount} areas and {outputItemsCount} items.");
		}

		static void Main(string[] args)
		{
			try
			{
				if (args.Length < 3)
				{
					Console.WriteLine("Usage: mmb-import <mudName> <inputFolder> <outputFolder>");
					Console.WriteLine("Example: mmb-import tbaMUD \"D:\\Projects\\chaos\\tbamud\\lib\\world\"");
					return;
				}

				Process(args[0], args[1], args[2]);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
