#nullable enable
using UnityEngine;
using UnityEngine.AI;

namespace RagsToRiches.View
{
    /// <summary>
    /// Тестовый NPC для перф-сцены: ходит по NavMesh к случайным точкам,
    /// имитируя нагрузку от агентов (avoidance + pathfinding).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcWanderer : MonoBehaviour
    {
        [SerializeField] private float _wanderRadius = 18f;
        [SerializeField] private float _repathIntervalSeconds = 3f;
        [SerializeField] private float _arriveDistance = 0.6f;

        private NavMeshAgent _agent = null!;
        private float _timer;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            bool arrived = !_agent.pathPending && _agent.remainingDistance < _arriveDistance;
            if (_timer > 0f && !arrived)
                return;

            PickNewDestination();
            _timer = _repathIntervalSeconds;
        }

        private void PickNewDestination()
        {
            Vector3 random = Random.insideUnitSphere * _wanderRadius;
            random.y = 0f;
            Vector3 candidate = transform.position + random;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
    }
}
