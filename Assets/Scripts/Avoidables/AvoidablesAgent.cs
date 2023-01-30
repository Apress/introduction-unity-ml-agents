using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Avoidables
{
	/// <summary>
	/// A rolling ball ML-Agent that uses it's action buffer to control the velocity of the ball.
	/// </summary>
	public class AvoidablesAgent : Agent
	{
		[SerializeField] 
		private float maxDistanceForCenterReward = 5;

		[SerializeField] 
		private float centerDistanceReward = 0.05f;
		
		[SerializeField]
		private float rewardPerStep = 0.05f;
		
		[SerializeField] 
		private UnityEvent onEpisodeBeginEvent;
		
		[SerializeField]
		private float spawnRadiusWhenReset = 10f;
		
		[SerializeField]
		private float maxSpeed = 5f;
		
		[SerializeField]
		private float acceleration = 3f;
		
		[SerializeField]
		private Rigidbody rb;

		[SerializeField] 
		private DemonstrationRecorder recorder;

		private float distanceToCenter;
		private float distanceToCenterPercentage;

		private void Start()
		{
			Time.timeScale = recorder.Record ? 1f : 10f;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(transform.position, spawnRadiusWhenReset);
			
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, maxDistanceForCenterReward);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (collision.gameObject.GetComponent<AvoidableObstacleController>())
			{
				FailAgent();
			}
		}

		private void FailAgent()
		{
			AddReward(-1f);
			onEpisodeBeginEvent?.Invoke();
			var spawnPosition = Random.insideUnitSphere * spawnRadiusWhenReset;
			var thisTransform = transform;
			var parent = thisTransform.parent;
			spawnPosition.y = parent.position.y + thisTransform.localScale.y / 2f;
			rb.position = parent.TransformPoint(spawnPosition);
			rb.velocity = Vector3.zero;
		}

		public override void Initialize()
		{
			var spawnPosition = Random.insideUnitSphere * spawnRadiusWhenReset;
			var thisTransform = transform;
			spawnPosition.y = thisTransform.parent.position.y + thisTransform.localScale.y / 2f;
			thisTransform.localPosition = spawnPosition;
			var maxReward = MaxStep * (rewardPerStep + centerDistanceReward);
			Debug.Log("Agent initialization complete. Theoretical max reward:" + maxReward);
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			sensor.AddObservation(transform.localPosition);
			sensor.AddObservation(rb.velocity);
			
			distanceToCenter = Vector3.Distance(transform.localPosition, Vector3.zero);
			distanceToCenterPercentage = distanceToCenter / maxDistanceForCenterReward;
			distanceToCenterPercentage = Mathf.Clamp01(distanceToCenterPercentage);
			distanceToCenterPercentage = 1f - distanceToCenter;
			sensor.AddObservation(distanceToCenter);
			sensor.AddObservation(distanceToCenterPercentage);
		}
		
		public override void OnActionReceived(ActionBuffers actionBuffer)
		{
			var inputX = Mathf.Clamp(actionBuffer.ContinuousActions[0], -1f, 1f);
			var inputZ = Mathf.Clamp(actionBuffer.ContinuousActions[1], -1f, 1f);
			
			rb.AddForce(inputX * acceleration, 0f, inputZ * acceleration, ForceMode.Force);
			AddReward(rewardPerStep);
			AddReward(centerDistanceReward * distanceToCenterPercentage);
		}
		
		public override void OnEpisodeBegin()
		{
			var spawnPosition = Random.insideUnitSphere * spawnRadiusWhenReset;
			var thisTransform = transform;
			var parent = thisTransform.parent;
			spawnPosition.y = parent.position.y + thisTransform.localScale.y / 2f;
			rb.position = parent.TransformPoint(spawnPosition);
			rb.velocity = Vector3.zero;
		}
		
		public override void Heuristic(in ActionBuffers actionsOut)
		{
			var continuousActionsOut = actionsOut.ContinuousActions;
			
			// Increase horizontal velocity.
			continuousActionsOut[0] = Input.GetAxis("Horizontal");
			// Increase vertical velocity.
			continuousActionsOut[1] = Input.GetAxis("Vertical");
		}

		private void FixedUpdate()
		{
			rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
			var thisTransform = transform;
			var position = thisTransform.localPosition;
			position.y = 0.25f;
			thisTransform.localPosition = position;
		}
	}
}
