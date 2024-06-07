using System;
using System.Collections.Generic;
using System.IO;

namespace MUDMapBuilder.Import.Diku
{
	public static class Importer
	{
		public static ImporterSettings Settings { get; private set; }

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
				var line = stream.ReadLine().Trim();
				if (line == "$~")
				{
					break;
				}

				var vnum = int.Parse(line.Substring(1));
				var name = stream.ReadDikuString();
				Log($"Processing room {name} (#{vnum})...");

				var room = new MMBRoom(vnum, name, false);

				var desc = stream.ReadDikuString();
				stream.ReadNumber(); // MMBArea Number

				line = stream.ReadLine();

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

		private static void ProcessObjects(Stream stream, MMBArea area)
		{
			var objects = new List<MMBObject>();
			while (!stream.EndOfStream())
			{
				var line = stream.ReadLine().Trim();
				if (line.StartsWith("$"))
				{
					break;
				}

				var vnum = int.Parse(line.Substring(1));
				if (vnum == 0)
				{
					break;
				}

				var name = stream.ReadDikuString();
				Log($"Processing object {name}...");

				var obj = new MMBObject
				{
					Id = vnum,
					Name = name,
					ShortDescription = stream.ReadDikuString(),
					Description = stream.ReadDikuString(),
				};

				var material = stream.ReadDikuString();

				obj.ItemType = stream.ReadEnumFromWord<ItemType>();
				if (Settings.SourceType == SourceType.Circle)
				{
					// 3 flags, each followed by 3 zeroes
					obj.ExtraFlags = (ItemExtraFlags)stream.ReadFlag();
					stream.ReadNumber(); stream.ReadNumber(); stream.ReadNumber();

					obj.WearFlags = (WearFlags)stream.ReadFlag();
					stream.ReadNumber(); stream.ReadNumber(); stream.ReadNumber();

					obj.AffectedByFlags = ((OldAffectedByFlags)stream.ReadFlag(1)).ToNewFlags();
					stream.ReadNumber(); stream.ReadNumber(); stream.ReadNumber();

					// 4 values
					obj.Value1 = stream.ReadNumber();
					obj.Value2 = stream.ReadNumber();
					obj.Value3 = stream.ReadNumber();
					obj.Value4 = stream.ReadNumber();

					// Rest
					stream.SkipWhitespace();
					line = stream.ReadLine();
					var parts = line.Split(' ');
					if (parts.Length > 0)
					{
						obj.Weight = int.Parse(parts[0].Trim());
					}

					if (parts.Length > 1)
					{
						obj.Cost = int.Parse(parts[1].Trim());
					}

					if (parts.Length > 3)
					{
						obj.Level = int.Parse(parts[3].Trim());
					}
				}
				else
				{

					obj.ExtraFlags = (ItemExtraFlags)stream.ReadFlag();
					obj.WearFlags = (WearFlags)stream.ReadFlag();

					if (Settings.SourceType == SourceType.ROM)
					{

						switch (obj.ItemType)
						{
							case ItemType.Weapon:
								obj.Value1 = (int)stream.ReadEnumFromWord<WeaponType>();
								obj.Value2 = stream.ReadNumber();
								obj.Value3 = stream.ReadNumber();
								obj.Value4 = (int)stream.ReadEnumFromWord<AttackType>();
								obj.Value5 = stream.ReadFlag();
								break;
							case ItemType.Container:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = stream.ReadFlag();
								obj.Value3 = stream.ReadNumber();
								obj.Value4 = stream.ReadNumber();
								obj.Value5 = stream.ReadNumber();
								break;
							case ItemType.DrinkContainer:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = stream.ReadNumber();
								obj.Value3 = (int)stream.ReadEnumFromWord<LiquidType>();
								obj.Value4 = stream.ReadNumber();
								obj.Value5 = stream.ReadNumber();
								break;
							case ItemType.Fountain:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = stream.ReadNumber();
								obj.Value3 = (int)stream.ReadEnumFromWord<LiquidType>();
								obj.Value4 = stream.ReadNumber();
								obj.Value5 = stream.ReadNumber();
								break;
							case ItemType.Wand:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = stream.ReadNumber();
								obj.Value3 = stream.ReadNumber();
								obj.Value4 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value5 = stream.ReadNumber();
								break;
							case ItemType.Staff:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = stream.ReadNumber();
								obj.Value3 = stream.ReadNumber();
								obj.Value4 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value5 = stream.ReadNumber();
								break;
							case ItemType.Potion:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value3 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value4 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value5 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								break;
							case ItemType.Pill:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value3 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value4 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value5 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								break;
							case ItemType.Scroll:
								obj.Value1 = stream.ReadNumber();
								obj.Value2 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value3 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value4 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								obj.Value5 = (int)stream.ReadEnumFromWordWithDef(Skill.Reserved);
								break;
							default:
								obj.Value1 = stream.ReadFlag();
								obj.Value2 = stream.ReadFlag();
								obj.Value3 = stream.ReadFlag();
								obj.Value4 = stream.ReadFlag();
								obj.Value5 = stream.ReadFlag();
								break;
						}

						obj.Level = stream.ReadNumber();
					}
					else
					{
						obj.S1 = stream.ReadDikuString();
						obj.S2 = stream.ReadDikuString();
						obj.S3 = stream.ReadDikuString();
						obj.S4 = stream.ReadDikuString();
					}

					obj.Weight = stream.ReadNumber();
					obj.Cost = stream.ReadNumber();

					if (Settings.SourceType == SourceType.Envy)
					{
						var costPerDay = stream.ReadNumber();
					}
					else
					{
						var letter = stream.ReadSpacedLetter();
						switch (letter)
						{
							case 'P':
								obj.Condition = 100;
								break;
							case 'G':
								obj.Condition = 90;
								break;
							case 'A':
								obj.Condition = 75;
								break;
							case 'W':
								obj.Condition = 50;
								break;
							case 'D':
								obj.Condition = 25;
								break;
							case 'B':
								obj.Condition = 10;
								break;
							case 'R':
								obj.Condition = 0;
								break;
							default:
								obj.Condition = 100;
								break;
						}
					}
				}

				while (!stream.EndOfStream())
				{
					var c = stream.ReadSpacedLetter();

					if (c == 'A')
					{
						var effect = new MMBEffect
						{
							EffectBitType = EffectBitType.Object,
							EffectType = (EffectType)stream.ReadNumber(),
							Modifier = stream.ReadNumber()
						};

						obj.Effects.Add(effect);
					}
					else if (c == 'F')
					{
						var effect = new MMBEffect();
						c = stream.ReadSpacedLetter();
						switch (c)
						{
							case 'A':
								effect.EffectBitType = EffectBitType.None;
								break;
							case 'I':
								effect.EffectBitType = EffectBitType.Immunity;
								break;
							case 'R':
								effect.EffectBitType = EffectBitType.Resistance;
								break;
							case 'V':
								effect.EffectBitType = EffectBitType.Vulnerability;
								break;
							default:
								stream.RaiseError($"Unable to parse effect bit '{c}'");
								break;
						}

						effect.EffectType = (EffectType)stream.ReadNumber();
						effect.Modifier = stream.ReadNumber();
						//effect.Bits = ((OldAffectedByFlags)stream.ReadFlag()).ToNewFlags();
						obj.Effects.Add(effect);
					}
					else if (c == 'E')
					{
						obj.ExtraKeyword = stream.ReadDikuString();
						obj.ExtraDescription = stream.ReadDikuString();
					}
					else if (c == 'L' || c == 'C')
					{
						var n = stream.ReadFlag();
					}
					else if (c == 'R' || c == 'D' || c == 'O' || c == 'X' || c == 'M' ||
						c == 'Y' || c == 'J' || c == 'G' || c == 'K' || c == 'V' || c == 'P' || c == 'd')
					{
					}
					else if (c == 'T')
					{
						// Circle trigger
						var n = stream.ReadNumber();
					}
					else
					{
						stream.GoBackIfNotEOF();
						break;
					}
				}

				objects.Add(obj);
			}

			area.Objects = objects.ToArray();
		}

		public static MMBArea ProcessFile(ImporterSettings settings, string areaFile)
		{
			Settings = settings;
			Log($"Processing {areaFile}...");

			var areaFileName = Path.GetFileName(areaFile);

			var worldFolder = Path.GetDirectoryName(Path.GetDirectoryName(areaFile));
			var zonPath = Path.Combine(worldFolder, "zon");
			zonPath = Path.Combine(zonPath, Path.ChangeExtension(areaFileName, "zon"));

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

			var objPath = Path.Combine(worldFolder, "obj");
			objPath = Path.Combine(objPath, Path.ChangeExtension(areaFileName, "obj"));
			if (File.Exists(objPath))
			{
				Log($"Loading objects from {objPath}");
				using (var stream = File.OpenRead(objPath))
				{
					ProcessObjects(stream, result);
				}
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