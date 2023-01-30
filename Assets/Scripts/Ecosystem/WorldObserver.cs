using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Ecosystem
{
	public static class WorldObserver
	{
		
		
		private static Dictionary<Vector2Int, HashSet<EcosystemAgent>> agentMap = new();
		private static Dictionary<EcosystemAgent, Vector2Int> trackedAgents = new();
		private static HashSet<Vector2Int> dirtyCells = new();
		private static RenderTexture renderTexture = null;
		private static int grazersAlive = 0;
		private static int predatorsAlive = 0;
		private static int huntersAlive = 0;

		// Cached values
		private static HashSet<Vector2Int> emptyCellQuery = new();
		
		private static Texture2D cachedTexture = null;
		private static Color[] cachedTextureColors = null;
		
		private static byte[] compressedObservation = null;
		private static int lastCompressedObservationUpdateFrame = -1;
		
		public static int GrazersAlive => grazersAlive;
		public static int PredatorsAlive => predatorsAlive;
		public static int HuntersAlive => huntersAlive;
		public static Dictionary<EcosystemAgent, Vector2Int> TrackedAgents => trackedAgents;


		public static bool GetCachedObservation(ref byte[] observation)
		{
			if (lastCompressedObservationUpdateFrame == Time.frameCount)
			{
				observation = compressedObservation;
				return true;
			}
			else
			{
				return false;
			}
		}
		
		public static void SetCachedObservation(byte[] observation)
		{
			compressedObservation = observation;
			lastCompressedObservationUpdateFrame = Time.frameCount;
		}

		public static IEnumerable<EcosystemAgent> GetAllAgents()
		{
			return trackedAgents.Keys;
		}
		
		public static bool IsPopulationFull()
		{
			return trackedAgents.Count >= EcosystemTrainingManager.MaxPopulation;
		}

		public static void AddOrUpdateAgent(EcosystemAgent agent)
		{
			if (trackedAgents.ContainsKey(agent))
			{
				RemoveAgent(agent);
			}
		
			var position = agent.transform.position;
			var agentLocation = new Vector2Int((int)Mathf.Floor(position.x), (int)Mathf.Floor(position.z));
			if (!agentMap.ContainsKey(agentLocation))
			{
				agentMap.Add(agentLocation, new HashSet<EcosystemAgent>());
			}

			agentMap[agentLocation].Add(agent);

			switch (agent.AgentSettings.Role)
			{
				case EcosystemRole.Grazer:
					grazersAlive++;
					break;
				case EcosystemRole.Predator:
					predatorsAlive++;
					break;
				case EcosystemRole.Hunter:
					huntersAlive++;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			trackedAgents.Add(agent, agentLocation);
			MarkCellAsDirty(agentLocation);
		}
	
		public static void RemoveAgent(EcosystemAgent agent, bool destroyAgent = false)
		{
			if (!trackedAgents.ContainsKey(agent))
			{
				return;
			}
			
			switch (agent.AgentSettings.Role)
			{
				case EcosystemRole.Grazer:
					grazersAlive--;
					break;
				case EcosystemRole.Predator:
					predatorsAlive--;
					break;
				case EcosystemRole.Hunter:
					huntersAlive--;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var agentLocation = trackedAgents[agent];
			agentMap[agentLocation].Remove(agent);
			trackedAgents.Remove(agent);
			MarkCellAsDirty(agentLocation);
			if (destroyAgent)
			{
				agent.EndEpisode();
				Object.Destroy(agent.gameObject);
			}
		}
		
		public static HashSet<EcosystemAgent> GetAgentsInCell(Vector2Int cell)
		{
			if (!agentMap.ContainsKey(cell))
			{
				agentMap.Add(cell, new HashSet<EcosystemAgent>());
			}

			return agentMap[cell];
		}
		
		public static IEnumerable<EcosystemAgent> GetAgentsInAdjacentCells(Vector2Int cell, int radius)
		{
			var agents = new HashSet<EcosystemAgent>();
			for (var x = -radius; x <= radius; x++)
			{
				for (var y = -radius; y <= radius; y++)
				{
					var adjacentCell = new Vector2Int(cell.x + x, cell.y + y);
					agents.UnionWith(GetAgentsInCell(adjacentCell));
				}
			}

			return agents;
		}
		
		public static bool IsCellEmpty(Vector2Int cell)
		{
			if (!agentMap.ContainsKey(cell))
			{
				agentMap.Add(cell, new HashSet<EcosystemAgent>());
			}
			
			return agentMap[cell].Count == 0;
		}
		
		public static Vector2 GetDistanceBetweenAgents(EcosystemAgent agent1, EcosystemAgent agent2)
		{
			var agent1Location = trackedAgents[agent1];
			var agent2Location = trackedAgents[agent2];
			return agent1Location - agent2Location;
		}
		
		public static Vector2Int PositionToCell(Vector3 position)
		{
			return new Vector2Int((int)Mathf.Floor(position.x), (int)Mathf.Floor(position.z));
		}

		private static void MarkCellAsDirty(Vector2Int location)
		{
			dirtyCells.Add(location);
		}
		
		public static void InitializeMapTexture(RenderTexture newRenderTexture)
		{
			renderTexture ??= newRenderTexture;
		}
		
		public static Vector3 CellToVector3(Vector2Int cell)
		{
			return new Vector3(cell.x, 0, cell.y);
		}
		
		public static Vector2Int GetRandomEmptyCell()
		{
			emptyCellQuery.Clear();
			foreach (var cell in agentMap.Keys)
			{
				if (agentMap[cell].Count == 0)
				{
					emptyCellQuery.Add(cell);
				}
			}

			if (emptyCellQuery.Count == 0)
			{
				return new Vector2Int(Random.Range(0, renderTexture.width), Random.Range(0, renderTexture.height));
			}
			
			return emptyCellQuery.ElementAt(Random.Range(0, emptyCellQuery.Count));
		}
		
		public static void RandomizeAgentPosition(EcosystemAgent agent)
		{
			var randomCell = GetRandomEmptyCell();
			agent.transform.position = CellToVector3(randomCell);
			agent.SetCell(randomCell);
			AddOrUpdateAgent(agent);
		}

		public static bool TryMoveAgent(EcosystemAgent agent, Vector2Int movementDelta)
		{
			var destination = GetCellForAgent(agent) + movementDelta;

			if (!IsCoordinateInBounds(destination))
			{
				return false;
			}
			
			if (destination == GetCellForAgent(agent))
			{
				return false;
			}

			if (agent.AgentSettings.Role == EcosystemRole.Grazer)
			{
				if (!IsCellEmpty(destination))
				{
					return false;
				}
			}else if (agent.AgentSettings.Role == EcosystemRole.Predator)
			{
				if (!IsCellEmpty(destination))
				{
					var agentsInCell = GetAgentsInCell(destination);
					
					foreach (var agentInCell in agentsInCell)
					{
						if (agentInCell.AgentSettings.Role == EcosystemRole.Grazer)
						{
							agentInCell.Die();
							agent.Feed(agent.AgentSettings.ReproductionValuePerFood);
							break;
						}
						else
						{
							return false;
						}
					}
				}
			}
			else if (agent.AgentSettings.Role == EcosystemRole.Hunter)
			{
			}

			

			agent.transform.localPosition = CellToVector3(destination);
			agent.SetCell(destination);
			AddOrUpdateAgent(agent);
			return true;
		}
		
		public static bool IsCoordinateInBounds(Vector2Int coordinate)
		{
			return coordinate.x >= 0 && coordinate.x < renderTexture.width && coordinate.y >= 0 && coordinate.y < renderTexture.height;
		}

		public static Vector2Int GetCellForAgent(EcosystemAgent agent)
		{
			if (trackedAgents.ContainsKey(agent))
			{
				return trackedAgents[agent];
			}
			else
			{
				AddOrUpdateAgent(agent);
			}
			
			return trackedAgents[agent];
		}

		public static HashSet<Vector2Int> GetEmptyAdjacentCells(Vector2Int cell, int radius)
		{
			emptyCellQuery.Clear();
			for (var x = -radius; x <= radius; x++)
			{
				for (var y = -radius; y <= radius; y++)
				{
					var adjacentCell = new Vector2Int(cell.x + x, cell.y + y);

					if (!IsCoordinateInBounds(adjacentCell))
					{
						continue;
					}
					
					if (IsCellEmpty(adjacentCell))
					{
						emptyCellQuery.Add(adjacentCell);
					}
				}
			}

			return emptyCellQuery;
		}
		
		public static void MoveAgentToCell(EcosystemAgent agent, Vector2Int cell)
		{
			agent.transform.localPosition = CellToVector3(cell);
			agent.SetCell(cell);
			AddOrUpdateAgent(agent);
		}
		
		public static void UpdateMapTexture()
		{
			if (renderTexture == null || dirtyCells.Count == 0)
			{
				return;
			}

			if (cachedTexture is null)
			{
				cachedTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
				cachedTexture.filterMode = FilterMode.Point;
				renderTexture.filterMode = FilterMode.Point;
				cachedTextureColors = new Color[renderTexture.width * renderTexture.height];
			}
			
			for (var x = 0; x < renderTexture.width; x++)
			{
				for (var y = 0; y < renderTexture.height; y++)
				{
					var cell = new Vector2Int(x, y);
					UpdateCellIfDirty(cell, cachedTextureColors, x, y);
				}
			}

			cachedTexture.SetPixels(cachedTextureColors);
			cachedTexture.Apply();
			Graphics.Blit(cachedTexture, renderTexture);
			dirtyCells.Clear();
		}

		private static void UpdateCellIfDirty(Vector2Int cell, Color[] colors, int x, int y)
		{
			if (!dirtyCells.Contains(cell))
			{
				return;
			}

			var color = colors[x + y * renderTexture.width];
			if (agentMap.ContainsKey(cell))
			{
				color = GetColorBasedOnAgents(GetAgentsInCell(cell));
			}

			colors[x + y * renderTexture.width] = color;
		}

		private static Color GetColorBasedOnAgents(HashSet<EcosystemAgent> cellAgents)
		{
			var color = Color.black;
			
			foreach (var cellAgent in cellAgents)
			{
				switch (cellAgent.AgentSettings.Role)
				{
					case EcosystemRole.Grazer:
						color += Color.green;
						break;
					case EcosystemRole.Predator:
						color += Color.red;
						break;
					case EcosystemRole.Hunter:
						color += Color.blue;
						break;
					default:
						break;
				}
			}

			return color;
		}
	}
}
