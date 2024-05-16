using System;
using System.IO;
using System.Reflection;

namespace MUDMapBuilder.Editor
{
	internal static class Utility
	{
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

		public static void QueueUIAction(Action action) => EditorGame.Instance.QueueUIAction(action);

		public static void SetStatusMessage(string message) => EditorGame.Instance.SetStatusMessage(message);
	}
}
