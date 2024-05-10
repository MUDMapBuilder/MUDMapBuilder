using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace MUDMapBuilder.Editor
{
	public class State
	{
		public const string StateFileName = "MUDMapBuilder.Editor.config";

		public static string StateFilePath
		{
			get
			{
				var result = Path.Combine(Utility.ExecutingAssemblyDirectory, StateFileName);
				return result;
			}
		}

		public Point Size { get; set; }
		public string EditedFile { get; set; }
		public int Step { get; set; }
		public AlgorithmUsage StraightenUsage { get; set; } = AlgorithmUsage.Use;
		public int StraightenSteps { get; set; }
		public AlgorithmUsage CompactUsage { get; set; } = AlgorithmUsage.Use;
		public int CompactSteps { get; set; }

		public State()
		{
		}

		public void Save()
		{
			using (var fileStream = File.Create(StateFilePath))
			{
				var xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8)
				{
					Formatting = Formatting.Indented
				};
				var serializer = new XmlSerializer(typeof(State));
				serializer.Serialize(xmlWriter, this);
			}
		}

		public static State Load()
		{
			if (!File.Exists(StateFilePath))
			{
				return null;
			}

			State state;
			using (var stream = new StreamReader(StateFilePath))
			{
				var serializer = new XmlSerializer(typeof(State));
				state = (State)serializer.Deserialize(stream);
			}

			return state;
		}
	}
}