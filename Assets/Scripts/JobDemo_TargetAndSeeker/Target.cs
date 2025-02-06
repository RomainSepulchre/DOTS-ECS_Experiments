using UnityEngine;

namespace Jobs.TargetAndSeekerDemo
{
    public class Target : MonoBehaviour
    {
        [SerializeField] private float speed;

        private Vector3 direction;

        private float timer = 0;

        void Start()
        {
            direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            timer = Random.Range(1f, 5f);
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                timer = Random.Range(1f, 5f);
            }

            // Update position
            transform.position = transform.position + (direction * speed * Time.deltaTime);
        }
    } 
}
