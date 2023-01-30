using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Ecosystem
{
	public class EcosystemAgent : Agent
	{
		[Header("Agent Settings")]
		[SerializeField]
		private EcosystemAgentSettings agentSettings;
		
		[SerializeField]
		private RenderTexture renderTexture;

		[Header("Events")]
		[SerializeField]
		private UnityEvent<EcosystemAgent> onAgentDied;

		private float currentEnergy = 0f;
		private float currentReproductionEnergy = 0f;
		private Vector2Int currentCellPosition;
		private Vector2Int movementDirection;
		
		private bool isInReproductionRange;

		public EcosystemAgentSettings AgentSettings => agentSettings;

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.magenta;

			// if this agent is of role grazer, draw a sphere around it to show the reproduction range
			if (agentSettings.Role == EcosystemRole.Grazer)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawCube(transform.position, Vector3.one);
			}else if (agentSettings.Role == EcosystemRole.Predator)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawCube(transform.position, Vector3.one);
			}else if (agentSettings.Role == EcosystemRole.Hunter)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawCube(transform.position, Vector3.one);
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			WorldObserver.InitializeMapTexture(renderTexture);
			currentEnergy = agentSettings.StartingEnergy;
			currentReproductionEnergy = 0f;
			WorldObserver.RandomizeAgentPosition(this);
		}

		public void FailAgent()
		{
			WorldObserver.RemoveAgent(this, true);
			EndEpisode();
		}
		
		private bool TrySpawnNewAgent()
		{
			if (WorldObserver.IsPopulationFull())
			{
				return false;
			}
			
			currentCellPosition = WorldObserver.GetCellForAgent(this);
			var emptyCells = WorldObserver.GetEmptyAdjacentCells(currentCellPosition, agentSettings.ReproductionSpawnRadius);
			
			if (emptyCells.Count == 0)
			{
				return false;
			}
			
			var emptyCell = emptyCells.ElementAt(Random.Range(0, emptyCells.Count));

			var newAgent = Instantiate(agentSettings.Prefab, WorldObserver.CellToVector3(emptyCell), Quaternion.identity);
			newAgent.transform.parent = transform.parent;
			newAgent.Initialize();
			WorldObserver.MoveAgentToCell(newAgent, emptyCell);

			return true;
		}
		
		public override void CollectObservations(VectorSensor sensor)
		{
			var cell = WorldObserver.PositionToCell(transform.localPosition);
			sensor.AddObservation(cell);
			sensor.AddOneHotObservation((int)agentSettings.Role, 3);
			sensor.AddObservation(currentEnergy);
			sensor.AddObservation(currentReproductionEnergy);
			sensor.AddObservation(isInReproductionRange);
		}
		
		public override void Heuristic(in ActionBuffers actionsOut)
		{
			var discreteActions = actionsOut.DiscreteActions;
			
			var horizontal = Input.GetAxis("Horizontal");
			var vertical = Input.GetAxis("Vertical");

			discreteActions[0] = ConvertMovementToAction(horizontal);
			discreteActions[1] = ConvertMovementToAction(vertical);
		}
		
		public override void OnActionReceived(ActionBuffers actionBuffers)
		{
			var movementXAction = actionBuffers.DiscreteActions[0];
			var movementZAction = actionBuffers.DiscreteActions[1];

			UpdateMovementDirection(movementXAction, movementZAction);

			TryMove();
			
			if (currentEnergy <= 0f)
			{
				if (agentSettings.Role == EcosystemRole.Grazer)
				{
					AddReward(5f);
				}
				
				FailAgent();
				return;
			}

			TryReproduce();

			if (agentSettings.Role == EcosystemRole.Grazer)
			{
				AddReward(agentSettings.RewardPerStep);
			}
		}

		public void SetCell(Vector2Int newPosition)
		{
			currentCellPosition = newPosition;
		}
		
		public void Feed(float amount)
		{
			currentReproductionEnergy += amount;
			AddReward(amount * 2);
		}

		public void Die()
		{
			if (agentSettings.Role == EcosystemRole.Grazer)
			{
				AddReward(-15f);
			}
			
			FailAgent();
		}

		private void TryReproduce()
		{
			switch (agentSettings.Role)
			{
				case EcosystemRole.Grazer:
				{
					RunGrazerReproductionRateChange();
					break;
				}
				case EcosystemRole.Predator:
				{
					break;
				}
			}

			if (currentReproductionEnergy >= agentSettings.ReproductionValueRequired)
			{
				if (TrySpawnNewAgent())
				{
					AddReward(agentSettings.RewardPerOffspring);
				}
				
				currentReproductionEnergy = 0f;
			}

			return;
		}

		private void RunGrazerReproductionRateChange()
		{
			var rateOfChange = agentSettings.ReproductionChangePerSecond * Time.deltaTime;

			var nearbyAgents =
				WorldObserver.GetAgentsInAdjacentCells(currentCellPosition, agentSettings.ReproductionRadius);

			var nearbyGrazers = nearbyAgents.Where(agent => agent.AgentSettings.Role == EcosystemRole.Grazer);

			if (nearbyGrazers.Count() > 1)
			{
				currentReproductionEnergy += rateOfChange;
				AddReward(rateOfChange);
				isInReproductionRange = true;
			}
			else
			{
				currentReproductionEnergy -= rateOfChange;
				isInReproductionRange = false;
			}
			
			currentReproductionEnergy = Mathf.Clamp(currentReproductionEnergy, 0f, agentSettings.ReproductionValueRequired);
		}

		private bool TryMove()
		{
			if (movementDirection.magnitude == 0f)
			{
				return false;
			}
			
			if (WorldObserver.TryMoveAgent(this, movementDirection))
			{
				currentEnergy -= agentSettings.EnergyLossPerMovement;
				return true;
			}
			
			currentEnergy -= agentSettings.EnergyLossPerStep;

			return false;
		}

		private void UpdateMovementDirection(int movementXAction, int movementZAction)
		{
			if (movementXAction == 0 && movementZAction == 0)
			{
				movementDirection = Vector2Int.zero;
				return;
			}

			var movementX = ConvertActionToMovement(movementXAction);
			var movementZ = ConvertActionToMovement(movementZAction);

			var movementXInt = (int)Mathf.Floor(movementX);
			var movementZInt = (int)Mathf.Floor(movementZ);
			
			movementDirection.x = movementXInt;
			movementDirection.y = movementZInt;
		}
		
		private float ConvertActionToMovement(int action)
		{
			return action switch
			{
				1 => -1f,
				2 => 1f,
				_ => 0f
			};
		}
		
		private int ConvertMovementToAction(float movement)
		{
			return movement switch
			{
				< 0 => 1,
				> 0 => 2,
				_ => 0
			};
		}
	}
}