using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Ecosystem
{
	public class EcosystemWorldSensor : MonoBehaviour, ISensor
	{
		[Serializable]
		private struct Observation
		{
			public readonly Vector2 Cell;
			public readonly EcosystemRole Role;
			
			public Observation(Vector2 cell, EcosystemRole role)
			{
				Cell = cell;
				Role = role;
			}
		}
		
		const int RoleCount = 3;
		const int ObservationSize = 5;
		const string SensorName = "WorldSensor";
		

		private readonly List<Observation> observations = new();
		private readonly IList<float> writerCache = new List<float>();
		

		public ObservationSpec GetObservationSpec()
		{
			return ObservationSpec.VariableLength(ObservationSize, EcosystemTrainingManager.MaxPopulation * ObservationSize);
		}

		public int Write(ObservationWriter writer)
		{
			var observationCount = observations.Count;
			var observationDeficit = EcosystemTrainingManager.MaxPopulation - observationCount;

			
			for (int i = 0; i < observationCount; i++)
			{
				var offset = i * ObservationSize;
				writer.AddList(CellToIList(observations[i].Cell), offset);
				writer.AddList(EncodeRoleAsOneHot(observations[i].Role), offset + 2);
			}
			
			for (int i = observationCount; i < observationDeficit; i++)
			{
				var offset = i * ObservationSize;
				writer.AddList(GetPaddedCell(), offset);
				writer.AddList(GetPaddedRole(), offset + 3);
			}


			return ObservationSize * EcosystemTrainingManager.MaxPopulation;
		}
		
		public IList<float> GetPaddedCell()
		{
			writerCache.Clear();
			writerCache.Add(0f);
			writerCache.Add(0f);
			return writerCache;
		}
		
		public IList<float> GetPaddedRole()
		{
			writerCache.Clear();
			for (int i = 0; i < RoleCount; i++)
			{
				writerCache.Add(0f);
			}
			return writerCache;
		}

		public IList<float> CellToIList(Vector2 cell)
		{
			writerCache.Clear();
			writerCache.Add(cell.x);
			writerCache.Add(cell.y);
			return writerCache;
		}
		
		public IList<float> EncodeRoleAsOneHot(EcosystemRole role)
		{
			writerCache.Clear();
			for (var i = 0; i < RoleCount; i++)
			{
				writerCache.Add(i == (int)role ? 1.0f : 0.0f);
			}
			return writerCache;
		}

		public byte[] GetCompressedObservation()
		{
			return null;
		}

		public void Update()
		{
			RefreshObservations();
		}
		
		private void RefreshObservations()
		{
			observations.Clear();
			foreach (var agent in WorldObserver.TrackedAgents)
			{
				var cell = agent.Value;
				var role = agent.Key.AgentSettings.Role;
				observations.Add(new Observation(cell, role));
			}
		}
		
		public void Reset()
		{
			observations.Clear();
		}

		public CompressionSpec GetCompressionSpec()
		{
			return CompressionSpec.Default();
		}

		public string GetName()
		{
			return SensorName;
		}
	}
}