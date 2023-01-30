using System;
using Unity.MLAgents;
using UnityEngine;

namespace Ecosystem
{
	[CreateAssetMenu(fileName = "New Ecosystem Settings", menuName = "Ecosystem/Ecosystem Settings File", order = 1)]
	[Serializable]
	public class EcosystemAgentSettings : ScriptableObject
	{
		[Header("Movement")]
		[SerializeField]
		private float movementSpeed = 5f;
	
		[Header("Rewards")]
		[SerializeField]
		private float rewardPerStep = 0.05f;
	
		[SerializeField]
		private float rewardPerOffspring = 0.1f;
	
		[Header("Reproduction")]
		[SerializeField]
		private float reproductionValueRequired = 1f;
	
		[SerializeField]
		private float reproductionChangePerSecond = 0.2f;
	
		[SerializeField]
		private int reproductionRadius = 3;
		
		[SerializeField]
		private int reproductionSpawnRadius = 2;
		
		[SerializeField]
		private int maxSpawnAttempts = 30;
		
		[SerializeField]
		private float reproductionValuePerFood = 0.75f;
		
		[Header("Energy")]
		[SerializeField]
		private float maxEnergy = 1f;
		
		[SerializeField]
		private float startingEnergy = 1f;
	
		[SerializeField]
		private float energyLossPerStep = 0.005f;
		
		[SerializeField]
		private float energyLossPerMovement = 0.02f;

		[Header("Role")]
		[SerializeField]
		private EcosystemRole role = EcosystemRole.Grazer;

		[Header("Collision")]
		[SerializeField]
		private LayerMask collisionMask = 0;

		[Header("Prefab")]
		[SerializeField]
		private EcosystemAgent prefab;
		
		public float MovementSpeed => movementSpeed;
		public float RewardPerStep => rewardPerStep;
		public float RewardPerOffspring => rewardPerOffspring;
		public float ReproductionValueRequired => reproductionValueRequired;
		public float ReproductionChangePerSecond => reproductionChangePerSecond;
		public int ReproductionRadius => reproductionRadius;
		public int ReproductionSpawnRadius => reproductionSpawnRadius;
		public int MaxSpawnAttempts => maxSpawnAttempts;
		public float MaxEnergy => maxEnergy;
		public float StartingEnergy => startingEnergy;
		public float EnergyLossPerStep => energyLossPerStep;
		public float EnergyLossPerMovement => energyLossPerMovement;
		public float ReproductionValuePerFood => reproductionValuePerFood;
		public EcosystemRole Role => role;
		public LayerMask CollisionMask => collisionMask;
		public EcosystemAgent Prefab => prefab;
	}
}
