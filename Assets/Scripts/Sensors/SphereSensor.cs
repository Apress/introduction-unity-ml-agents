using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Sensors
{
	public class SphereSensor : MonoBehaviour, ISensor
	{
		[Serializable]
		private struct Observation
		{
			public readonly Vector3 Position;
			public readonly Vector3 Velocity;
			
			public Observation(Vector3 position, Vector3 velocity)
			{
				Position = position;
				Velocity = velocity;
			}
		}
		
		const int ObservationSize = 6;
		const string SensorName = "SphereSensor";
		
		[SerializeField]
		[Tooltip("The maximum number of observations to record, exclusive.")]
		private int maximumObservations = 10;
		
		[SerializeField]
		[Tooltip("The maximum range that this agent can detect other entities on the sensorLayerMask.")]
		private float sensorRange = 20f;
		
		[SerializeField]
		[Tooltip("Choose the layer that the sensor will detect objects on excluding itself.")]
		private LayerMask sensorLayerMask;

		private IList<Observation> observations;
		private List<Rigidbody> detectedObjects;
		private Collider[] detectionBuffer;

		private void Start()
		{
			observations = new List<Observation>();
			detectedObjects = new List<Rigidbody>();
			detectionBuffer = new Collider[maximumObservations * 2];
		}

		public ObservationSpec GetObservationSpec()
		{
			return ObservationSpec.VariableLength(ObservationSize, maximumObservations);
		}

		public int Write(ObservationWriter writer)
		{
			var observationDeficit = maximumObservations - observations.Count;
			var observationCount = observations.Count;
			
			for (int i = 0; i < observationCount; i++)
			{
				var offset = i * ObservationSize;
				writer.Add(observations[i].Position, offset);
				writer.Add(observations[i].Velocity, offset + 3);
			}
			
			for (int i = observationCount; i < observationDeficit; i++)
			{
				var offset = i * ObservationSize;
				writer.Add(Vector3.zero, offset);
				writer.Add(Vector3.zero, offset + 3);
			}


			return ObservationSize * maximumObservations;
		}

		public byte[] GetCompressedObservation()
		{
			return null;
		}

		public void Update()
		{
			RefreshDetectedObjects();
			RefreshNearestObservations();
		}

		private void RefreshNearestObservations()
		{
			detectedObjects = detectedObjects.OrderBy(
					match => Vector3.Distance(match.transform.localPosition, transform.localPosition)).ToList();
			
			observations.Clear();

			for (int i = 0, count = detectedObjects.Count; i < count && i < maximumObservations; i++)
			{
				var detectedObject = detectedObjects[i];
				var relativePosition = detectedObject.transform.localPosition - transform.localPosition;
				observations.Add(new Observation(relativePosition, detectedObject.velocity));
			}
		}

		private void RefreshDetectedObjects()
		{
			detectedObjects.Clear();
			var size = Physics.OverlapSphereNonAlloc(transform.position, sensorRange, detectionBuffer, sensorLayerMask);

			for (var i = 0; i < size; i++)
			{
				var detectedObject = detectionBuffer[i].attachedRigidbody;
				detectedObjects.Add(detectedObject);
			}
		}

		public void Reset()
		{
			Array.Clear(detectionBuffer, 0, detectionBuffer.Length);
			observations.Clear();
			detectedObjects.Clear();
		}

		public CompressionSpec GetCompressionSpec()
		{
			return CompressionSpec.Default();
		}

		public string GetName()
		{
			return SensorName;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, sensorRange);

			if (detectedObjects == null)
			{
				return;
			}
			
			for (int i = 0, count = detectedObjects.Count; i < count && i < maximumObservations; i++)
			{
				var detectedObject = detectedObjects[i];
				Gizmos.color = Color.green;
				Gizmos.DrawLine(transform.position, detectedObject.transform.position);
			}
			
			for (int i = maximumObservations, count = detectedObjects.Count; i < count; i++)
			{
				var detectedObject = detectedObjects[i];
				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, detectedObject.transform.position);
			}
		}
	}
}