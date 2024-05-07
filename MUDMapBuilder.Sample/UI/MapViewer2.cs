using AbarimMUD.Data;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MUDMapBuilder.Sample.UI
{
	internal class MapViewer2 : Widget
	{
		private static readonly Vector2 RoomSize = new Vector2(20, 20);
		private static readonly Vector2 TopLeft = new Vector2(200, 200);
		private const int RefreshRateInMs = 10;

		private class Vertex
		{
			public Room Room;
			public Vector2 NetForce;
			public Vector2 Position;
		}

		private Area _map;
		private readonly List<Vertex> _vertices = new List<Vertex>();
		private DateTime? _lastUpdate = null;

		public Area Map
		{
			get => _map;

			set
			{
				if (value == _map)
				{
					return;
				}

				_map = value;
				Rebuild();
			}
		}

		private void Rebuild()
		{
			_vertices.Clear();

			foreach (var room in _map.Rooms)
			{
				var vertex = new Vertex
				{
					Room = room,
					Position = new Vector2(TopLeft.X + Utility.Random.Next(0, 100), TopLeft.Y + Utility.Random.Next(0, 100))
				};

				_vertices.Add(vertex);
			}

			InvalidateMeasure();

			_lastUpdate = null;
		}

		protected override Point InternalMeasure(Point availableSize)
		{
			var max = Vector2.Zero;
			foreach (var vertex in _vertices)
			{
				if (vertex.Position.X + RoomSize.X > max.X)
				{
					max.X = vertex.Position.X + RoomSize.X;
				}

				if (vertex.Position.Y + RoomSize.Y > max.Y)
				{
					max.Y = vertex.Position.Y + RoomSize.Y;
				}
			}

			return new Point((int)max.X, (int)max.Y);
		}

		public override void InternalRender(RenderContext context)
		{
			base.InternalRender(context);

			foreach (var vertex in _vertices)
			{
				context.DrawRectangle(new Rectangle((int)vertex.Position.X, (int)vertex.Position.Y, (int)RoomSize.X, (int)RoomSize.Y),
					Color.Black, 1);

				foreach (var exit in vertex.Room.Exits)
				{
					if (exit.TargetRoom == null || exit.TargetRoom.AreaId != _map.Id)
					{
						continue;
					}

					var targetVertex = (from v in _vertices where v.Room == exit.TargetRoom select v).FirstOrDefault();
					if (targetVertex == null)
					{
						continue;
					}

					context.DrawLine(vertex.Position + RoomSize / 2,
						targetVertex.Position + RoomSize / 2, Color.Black);
				}
			}

			UpdateVertices();
		}

		private void UpdateVertices()
		{
			for (var i = 0; i < _vertices.Count; i++)
			{
				_vertices[i].NetForce = Vector2.Zero;

				// Repulsion
				for (var j = 0; j < _vertices.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					var v = _vertices[i].Position - _vertices[j].Position;
					var rsq = v.LengthSquared();

					if (rsq < 0.1f)
					{
						rsq = 0.1f;
					}

					_vertices[i].NetForce.X += 1.0f * v.X / rsq;
					_vertices[i].NetForce.Y += 1.0f * v.Y / rsq;
				}

				// Attraction
				for (var j = 0; j < _vertices.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					var connection = (from ex in _vertices[i].Room.Exits where ex.TargetRoom == _vertices[j].Room select ex).FirstOrDefault();

					if (connection != null)
					{
						var v = _vertices[j].Position - _vertices[i].Position + connection.Direction.GetOppositeDirection().GetDelta();

						_vertices[i].NetForce.X += 0.01f * v.X;
						_vertices[i].NetForce.Y += 0.01f * v.Y;
					}
				}

				_vertices[i].Position += _vertices[i].NetForce;
			}

			// Shift
			var shift = Vector2.Zero;
			bool minSet = false;
			for (var i = 0; i < _vertices.Count; i++)
			{
				var v = _vertices[i];

				if (!minSet)
				{
					shift = v.Position;
					minSet = true;
				}
				else
				{
					if (v.Position.X < shift.X)
					{
						shift.X = v.Position.X;
					}

					if (v.Position.Y < shift.Y)
					{
						shift.Y = v.Position.Y;
					}
				}
			}

			shift -= TopLeft;
			for (var i = 0; i < _vertices.Count; i++)
			{
				var v = _vertices[i];

				v.Position -= shift;
			}


			InvalidateMeasure();
		}
	}
}
