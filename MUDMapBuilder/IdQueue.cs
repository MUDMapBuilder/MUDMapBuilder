using System.Collections.Generic;

namespace MUDMapBuilder
{
	internal class IdQueue
	{
		private readonly List<int> _toProcess = new List<int>();
		private readonly HashSet<int> _toProcessSet = new HashSet<int>();
		private readonly HashSet<int> _processed = new HashSet<int>();

		public int Count => _toProcess.Count;

		public HashSet<int> Processed => _processed;

		public IdQueue()
		{
		}

		public IdQueue(int firstId)
		{
			Add(firstId);
		}

		public IdQueue(int[] firstIds)
		{
			foreach (var id in firstIds)
			{
				Add(id);
			}
		}

		public void Add(int id)
		{
			if (_toProcessSet.Contains(id))
			{
				return;
			}
			
			_toProcess.Add(id);
			_toProcessSet.Add(id);
		}
		public void Remove(int id)
		{
			_toProcess.RemoveAll(i => i == id);
			_toProcessSet.Remove(id);
		}

		public bool WasAdded(int id) => _toProcessSet.Contains(id);
		public bool WasProcessed(int id) => _processed.Contains(id);

		public int Pop()
		{
			var id = _toProcess[0];
			_toProcess.RemoveAt(0);
			_toProcessSet.Remove(id);
			_processed.Add(id);

			return id;
		}
	}
}
	