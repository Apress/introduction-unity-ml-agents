using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.MLAgents;
using UnityEngine;

namespace Ecosystem
{
    public class EcosystemTrainingManager : MonoBehaviour
    {
        public static EcosystemTrainingManager Instance { get; private set; }

        public static int MaxPopulation => Instance.maxPopulation - Instance.minimumGrazerPopulation - Instance.minimumPredatorPopulation;
        
        [SerializeField]
        private Transform trainingEnvironment;
        
        [SerializeField]
        private EcosystemAgent grazerAgentPrefab;
        
        [SerializeField]
        private int grazerStartingPopulation = 10;
        
        [SerializeField]
        private EcosystemAgent predatorAgentPrefab;
        
        [SerializeField]
        private int predatorStartingPopulation = 10;
        
        [SerializeField]
        private EcosystemAgent hunterAgentPrefab;

        [SerializeField]
        private int tickRate = 20;

        [SerializeField]
        private int maxTicks = 100000;
        
        [SerializeField]
        private int maxPopulation = 200;
        
        [SerializeField]
        private int minimumGrazerPopulation = 10;
        
        [SerializeField]
        private int minimumPredatorPopulation = 10;
        
        private int ticksSinceLastReset = 0;

        public float TickRateTime => 1f / tickRate * 100f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        
        private void Start()
        {
            InitializeTrainingRound();
            TickLoop();
        }

        private void InitializeTrainingRound()
        {
            RemoveAllAgents();
            SpawnBaseAgents();
            WorldObserver.UpdateMapTexture();
        }
        
        private void RemoveAllAgents()
        {
            var agents = WorldObserver.GetAllAgents().ToList();
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                WorldObserver.RemoveAgent(agents[i], true);
            }
        }
        
        private void SpawnBaseAgents()
        {
            SpawnAgents(grazerAgentPrefab, grazerStartingPopulation);
            SpawnAgents(predatorAgentPrefab, predatorStartingPopulation);
        }
        
        private void SpawnAgents(EcosystemAgent agentPrefab, int count)
        {
            for (int i = 0; i < count; i++)
            {
                EcosystemAgent agent = Instantiate(agentPrefab, trainingEnvironment, true);
                agent.Initialize();
            }
        }
        
        private async void TickLoop()
        {
            var isTicking = true;
            while (isTicking)
            {
                if (ticksSinceLastReset >= maxTicks)
                {
                    ticksSinceLastReset = 0;
                    RemoveAllAgents();
                    SpawnBaseAgents();
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(TickRateTime));
                
                if (WorldObserver.GrazersAlive < minimumGrazerPopulation)
                {
                    SpawnAgents(grazerAgentPrefab, minimumGrazerPopulation - WorldObserver.GrazersAlive);
                }
                
                if (WorldObserver.PredatorsAlive < minimumPredatorPopulation)
                {
                    SpawnAgents(predatorAgentPrefab, minimumPredatorPopulation - WorldObserver.PredatorsAlive);
                }
                
                foreach (var agent in WorldObserver.GetAllAgents())
                {
                    agent.RequestDecision();
                }
                WorldObserver.UpdateMapTexture();
                
                ticksSinceLastReset++;
            }
            
        }
    }
}
