using System;
using System.Collections.Generic;
using System.IO;

namespace MUDMapBuilder.Import.Diku
{
	public static class Importer
	{
		public static void Log(string message)
		{
			Console.WriteLine(message);
		}

		private static MMBArea ProcessMMBArea(Stream stream)
		{
			var vnum = int.Parse(stream.ReadId());
			var credits = stream.ReadDikuString();
			var name = stream.ReadDikuString();

			var result =            new MMBArea
			{
				Name = name.Replace('/', '_'),
				Credits = credits,
			};

			if (stream.EndOfStream())
			{
				return result;
			}

			var line = stream.ReadLine();
			var parts = line.Split(' ');

			if (parts.Length > 8)
			{
				result.MinimumLevel = parts[8].Trim();
			}

			if (parts.Length > 9)
			{
				result.MaximumLevel = parts[9].Trim();
			}

			return result;
		}

		private static MMBRoom[] ProcessMMBRooms(Stream stream)
		{
			var result = new List<MMBRoom>();
			while (!stream.EndOfStream())
			{
				var vnum = int.Parse(stream.ReadId());
				var name = stream.ReadDikuString();
				Log($"Processing room {name} (#{vnum})...");

				var room = new MMBRoom(vnum, name, false);

				var desc = stream.ReadDikuString();
				stream.ReadNumber(); // MMBArea Number

				var line = stream.ReadLine();

				result.Add(room);
				char c;
				while (!stream.EndOfStream())
				{
					c = stream.ReadSpacedLetter();

					if (c == 'S')
					{
						// End of room
						break;
					}
					else if (c == 'H')
					{
						var healRate = stream.ReadNumber();
					}
					else if (c == 'M')
					{
						var manaRate = stream.ReadNumber();
					}
					else if (c == 'C')
					{
						string clan = stream.ReadDikuString();
					}
					else if (c == 'D')
					{
						var direction = (MMBDirection)stream.ReadNumber();
						var exitDesc = stream.ReadDikuString();
						var keyword = stream.ReadDikuString();

						var locks = stream.ReadNumber();
						var keyVNum = stream.ReadNumber();
						var targetVnum = stream.ReadNumber();
						if (targetVnum != -1)
						{
							room.Connections[direction] = new MMBRoomConnection(direction, targetVnum);
						}
					}
					else if (c == 'E')
					{
						var extraKeyword = stream.ReadDikuString();
						var extraDescription = stream.ReadDikuString();
					}
					else if (c == 'O')
					{
						var owner = stream.ReadDikuString();
					}
					else
					{
						throw new Exception($"Unknown room command '{c}'");
					}
				}

				var doRun = true;
				while (!stream.EndOfStream() && doRun)
				{
					c = stream.ReadSpacedLetter();
					switch (c)
					{
						case '$':
							goto finish;
						case 'T':
							var triggerId = stream.ReadNumber();
							break;
						default:
							stream.Seek(-1, SeekOrigin.Current);
							doRun = false;
							break;
					}
				}
			}
		finish:;
			return result.ToArray();
		}

		public static MMBArea ProcessFile(string areaFile)
		{
			Log($"Processing {areaFile}...");

			// var zonPath = Path.ChangeExtension(areaFile, "zon");
			var zonPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(areaFile)), "zon");
			zonPath = Path.Combine(zonPath, Path.ChangeExtension(Path.GetFileName(areaFile), "zon"));

			MMBArea result;

			if (File.Exists(zonPath))
			{
				Log($"Getting area name from {zonPath}...");
				using (var stream = File.OpenRead(zonPath))
				{
					result = ProcessMMBArea(stream);
					Log($"Area name is '{result.Name}'");
				}
			}
			else
			{
				result = new MMBArea
				{
					Name = Path.GetFileName(areaFile).Replace('/', '_')
				};
			}

			using (var stream = File.OpenRead(areaFile))
			{
				var rooms = ProcessMMBRooms(stream);
				foreach (var room in rooms)
				{
					result.Add(room);
				}
			}

			return result;
		}
	}
}