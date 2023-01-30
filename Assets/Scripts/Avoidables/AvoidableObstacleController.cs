using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Avoidables
{
    public class AvoidableObstacleController : MonoBehaviour
    {
        [SerializeField]
        private float speed = 2f;
        
        [SerializeField]
        private Rigidbody rb;
        
        private Vector3 direction;

        private void OnEnable()
        {
            direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            rb.velocity = direction * speed;
        }

        private void FixedUpdate()
        {
            rb.velocity = direction * speed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            direction = Vector3.Reflect(direction, collision.contacts[0].normal);
            direction.y = 0;
            
            rb.velocity = direction * speed;
        }
    }
}
