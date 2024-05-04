using AbarimMUD.Data;

namespace MUDMapBuilder.Sample
{
	public static class Database
	{
		public static string ConnectionString { get; set; } = "Data Source=D:\\Projects\\AbarimMUD\\Data\\database.db";

		public static DataContext CreateDataContext() => new DataContext(ConnectionString);
	}
}
