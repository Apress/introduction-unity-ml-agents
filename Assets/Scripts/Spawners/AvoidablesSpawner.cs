using UnityEngine;
using Random = UnityEngine.Random;

namespace Spawners
{
	public class AvoidablesSpawner : MonoBehaviour
	{
		[SerializeField]
		private GameObject avoidablePrefab;
	
		[SerializeField]
		private Transform avoidablesPoolParent;
	
		[SerializeField]
		private LayerMask avoidableLayerMask;
	
		[SerializeField]
		private int maxAvoidables = 10;
	
		[SerializeField]
		private float spawnRadius = 10f;
	
		private GameObject[] avoidablesPool;
		private int avoidablesPoolIndex = 0;
		private float spawnDelay = 0.1f;
		private float spawnTimer = 0f;
		private bool isSpawning = true;
		private int activeAvoidables = 0;
	
		private void Start ()
		{
			avoidablesPool = new GameObject[maxAvoidables];
			for (int i = 0; i < maxAvoidables; i++)
			{
				avoidablesPool[i] = Instantiate(avoidablePrefab, avoidablesPoolParent);
				avoidablesPool[i].SetActive(false);
			}
		}
	
		private void Update ()
		{
			if (!isSpawning)
			{
				return;
			}

			spawnTimer += Time.deltaTime;
			if (spawnTimer >= spawnDelay && activeAvoidables < maxAvoidables)
			{
				spawnTimer = 0f;
				Spawn();
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, spawnRadius);
		}

		private void Spawn()
		{
			var validSpawnLocationFound = false;
			var maxAttempts = 10;
			while (!validSpawnLocationFound)
			{
				Vector3 spawnPosition = Random.insideUnitSphere * spawnRadius;
				spawnPosition.y = transform.position.y + avoidablePrefab.transform.localScale.y / 2f;
				spawnPosition = transform.TransformPoint(spawnPosition);
				RaycastHit hit;
				var raycastOrigin = spawnPosition + (Vector3.up * 2f);
				raycastOrigin = avoidablesPoolParent.TransformPoint(raycastOrigin);

				var obstructionRadius = 10f;
				if (Physics.SphereCast(raycastOrigin, obstructionRadius , Vector3.down, out hit, 4f, avoidableLayerMask))
				{
					maxAttempts--;
				
					if (maxAttempts <= 0)
					{
						break;
					}
				
					continue;
				}
				
				avoidablesPool[avoidablesPoolIndex].transform.position = spawnPosition;
				avoidablesPool[avoidablesPoolIndex].SetActive(true);
				avoidablesPoolIndex++;
				activeAvoidables++;
				if (avoidablesPoolIndex >= maxAvoidables)
				{
					avoidablesPoolIndex = 0;
				}
			
				validSpawnLocationFound = true;
			}

		}

		public void ResetSpawning()
		{
			for (int i = 0; i < maxAvoidables; i++)
			{
				avoidablesPool[i].SetActive(false);
			}
			
			activeAvoidables = 0;
			avoidablesPoolIndex = 0;
		}
	}
}
