using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MUDMapBuilder.Import.Diku
{
	internal static class Utility
	{
		private static readonly Dictionary<OldResistanceFlags, ResistanceFlags> _resistanceFlagsMapper = new Dictionary<OldResistanceFlags, ResistanceFlags>();
		private static readonly Dictionary<OldAffectedByFlags, AffectedByFlags> _affectedByFlagsMapper = new Dictionary<OldAffectedByFlags, AffectedByFlags>();


		public static string ExecutingAssemblyDirectory
		{
			get
			{
				string codeBase = Assembly.GetExecutingAssembly().Location;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		static Utility()
		{
			PopulateMapper(_affectedByFlagsMapper);
			PopulateMapper(_resistanceFlagsMapper);
		}

		private static void PopulateMapper<T2, T>(Dictionary<T2, T> mapper) where T : struct, Enum where T2 : struct, Enum
		{
			foreach (T2 flag in Enum.GetValues(typeof(T2)))
			{
				if ((int)(object)flag == 0)
				{
					continue;
				}

				var name = flag.ToString();

				T newFlag;
				if (Enum.TryParse(name, true, out newFlag))
				{
					mapper[flag] = newFlag;
				}
			}
		}

		public static void RaiseError(this Stream stream, string message)
		{
			var line = 0;
			var linePos = 0;

			var lastPos = stream.Position;

			// Calculate the line and line pos
			stream.Seek(0, SeekOrigin.Begin);

			while (stream.Position < lastPos)
			{
				var b = stream.ReadByte();

				++linePos;

				if (b == '\n')
				{
					++line;
					linePos = 0;
				}
			}

			var asFileStream = stream as FileStream;
			if (asFileStream != null)
			{
				var fileName = asFileStream.Name;
				throw new Exception($"File: {fileName}, Line: {line}, LinePos: {linePos}, Error: {message}");
			}

			throw new Exception($"Line: {line}, LinePos: {linePos}, Error: {message}");
		}

		public static string RemoveTilda(this string str) => str.Replace("~", "");

		public static int ParseVnum(this string str) => int.Parse(str.Substring(1));


		public static bool EndOfStream(this Stream stream) => stream.Position >= stream.Length;

		public static void GoBackIfNotEOF(this Stream stream)
		{
			if (stream.Position == 0 || stream.EndOfStream())
			{
				return;
			}

			stream.Seek(-1, SeekOrigin.Current);
		}

		public static char ReadLetter(this Stream stream) => (char)stream.ReadByte();

		public static char ReadSpacedLetter(this Stream stream)
		{
			char c;
			do
			{
				c = stream.ReadLetter();
			}
			while (!stream.EndOfStream() && char.IsWhiteSpace(c));

			return c;
		}

		public static string ReadLine(this Stream stream)
		{
			var sb = new StringBuilder();

			var endOfLine = false;
			while (!stream.EndOfStream())
			{
				var c = stream.ReadLetter();

				var isNewLine = c == '\n' || c == '\r';
				if (!endOfLine)
				{
					if (!isNewLine)
					{
						sb.Append(c);
					}
					else
					{
						endOfLine = true;
					}
				}
				else if (!isNewLine)
				{
					stream.GoBackIfNotEOF();
					break;
				}

			}

			return sb.ToString();
		}


		public static string ReadId(this Stream stream)
		{
			var c = stream.ReadSpacedLetter();
			if (c != '#')
			{
				stream.RaiseError("# not found");
			}

			return stream.ReadLine().Trim();
		}

		public static void SkipWhitespace(this Stream stream)
		{
			while(!stream.EndOfStream())
			{
				var c = (char)stream.ReadByte();
				if (!char.IsWhiteSpace(c))
				{
					stream.GoBackIfNotEOF();
					break;
				}
			}
		}

		public static int ReadFlag(this Stream stream, int asciiAddition = 0)
		{
			stream.SkipWhitespace();

			var sb = new StringBuilder();
			while(!stream.EndOfStream())
			{
				var c = (char)stream.ReadByte();
				if (char.IsWhiteSpace(c))
				{
					break;
				}

				sb.Append(c);
			}

			var str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}

			var negative = false;
			if (str.StartsWith("-"))
			{
				negative = true;
				str = str.Substring(0);
			}

			int result = 0;
			if (int.TryParse(str, out result))
			{ 
			} else
			{
				// ASCII flags
				for(var i = 0; i < str.Length; ++i)
				{
					var c = str[i];
					if ('a' <= c && c <= 'z')
					{
						result |= 1 << (c - 'a' + asciiAddition);
					} else if ('A' <= c && c <= 'Z')
					{
						result |= 1 << (26 + c - 'a' + asciiAddition);
					} else
					{
						stream.RaiseError($"Could not parse ascii flag {str}");
					}
				}
			}

			return negative ? -result : result;
		}

		public static int ReadNumber(this Stream stream)
		{
			var negative = false;
			var c = stream.ReadSpacedLetter();

			if (c == '+')
			{
				c = stream.ReadLetter();
			}
			else if (c == '-')
			{
				negative = true;
				c = stream.ReadLetter();
			}

			if (!char.IsDigit(c))
			{
				stream.RaiseError($"Could not parse number {c}");
			}

			var sb = new StringBuilder();

			while (!stream.EndOfStream() && char.IsDigit(c))
			{
				sb.Append(c);
				c = stream.ReadLetter();
			}

			var result = int.Parse(sb.ToString());

			if (negative)
			{
				result = -result;
			}

			if (c == '|')
			{
				result += stream.ReadNumber();
			}

			// Last symbol beint to the new data
			stream.GoBackIfNotEOF();

			return result;
		}


		public static string ReadDikuString(this Stream stream)
		{
			var result = new StringBuilder();

			while (!stream.EndOfStream())
			{
				var line = stream.ReadLine();

				var lastLine = line.EndsWith("~");
				if (lastLine)
				{
					line = line.Substring(0, line.Length - 1);
				}

				if (!string.IsNullOrEmpty(line))
				{
					result.Append(line);
				}

				if (lastLine)
				{
					break;
				}
			}

			return result.ToString();
		}

		public static char EnsureChar(this Stream stream, char expected)
		{
			var c = stream.ReadLetter();
			if (c != expected)
			{
				stream.RaiseError($"Expected symbol '{expected}'");
			}

			return c;
		}

		public static string ReadDice(this Stream stream)
		{
			var sb = new StringBuilder();
			sb.Append(stream.ReadNumber());
			sb.Append(stream.EnsureChar('d'));
			sb.Append(stream.ReadNumber());
			sb.Append(stream.EnsureChar('+'));
			sb.Append(stream.ReadNumber());

			return sb.ToString();
		}

		public static string ReadWord(this Stream stream)
		{
			if (stream.EndOfStream())
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			var c = stream.ReadSpacedLetter();

			var startsWithQuote = c == '"' || c == '\'';

			if (startsWithQuote)
			{
				c = stream.ReadLetter();
			}

			while (!stream.EndOfStream())
			{
				if ((startsWithQuote && (c == '"' || c == '\'')) ||
					(!startsWithQuote && char.IsWhiteSpace(c)))
				{
					break;
				}

				sb.Append(c);
				c = stream.ReadLetter();
			}

			if (!startsWithQuote)
			{
				stream.GoBackIfNotEOF();
			}

			return sb.ToString();
		}

		public static T ToEnum<T>(this Stream stream, string value)
		{
			value = value.Replace("_", "").Replace(" ", "");

			// Parse the enum
			try
			{
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception)
			{
				stream.RaiseError($"Enum parse error: enum type={typeof(T).Name}, value={value}");
			}

			return default(T);
		}

		public static T ReadEnumFromDikuString<T>(this Stream stream)
		{
			var str = stream.ReadDikuString();
			return stream.ToEnum<T>(str);
		}

		public static T ReadEnumFromDikuStringWithDef<T>(this Stream stream, T def)
		{
			var str = stream.ReadDikuString();

			if (string.IsNullOrEmpty(str))
			{
				return def;
			}

			return stream.ToEnum<T>(str);
		}

		public static T ReadEnumFromWord<T>(this Stream stream)
		{
			var word = stream.ReadWord();
			return stream.ToEnum<T>(word);
		}

		public static T ReadEnumFromWordWithDef<T>(this Stream stream, T def)
		{
			var word = stream.ReadWord();

			if (string.IsNullOrEmpty(word))
			{
				return def;
			}

			return stream.ToEnum<T>(word);
		}

		public static bool ReadSocialString(this Stream stream, ref string s)
		{
			var temp = stream.ReadLine().Trim();
			if (temp == "$")
			{
				s = string.Empty;
			}
			else if (temp == "#")
			{
				return false;
			}
			else
			{
				s = temp;
			}

			return true;
		}

		private static HashSet<T> ConvertFlags<T, T2>(Dictionary<T2, T> mapper, T2 flags) where T2 : struct, Enum
		{
			var result = new HashSet<T>();

			if ((int)(object)flags == 0)
			{
				// None
				return new HashSet<T>();
			}

			foreach (T2 flag in Enum.GetValues(typeof(T2)))
			{
				if ((int)(object)flag == 0)
				{
					continue;
				}

				if (flags.HasFlag(flag))
				{
					T newFlag;
					if (!mapper.TryGetValue(flag, out newFlag))
					{
						throw new Exception($"Unable to find the corresponding flag for {flag}");
					}

					result.Add(newFlag);
				}
			}

			return result;
		}

		public static HashSet<ResistanceFlags> ToNewFlags(this OldResistanceFlags flags) => ConvertFlags(_resistanceFlagsMapper, flags);
		public static HashSet<AffectedByFlags> ToNewFlags(this OldAffectedByFlags flags) => ConvertFlags(_affectedByFlagsMapper, flags);

	}
}