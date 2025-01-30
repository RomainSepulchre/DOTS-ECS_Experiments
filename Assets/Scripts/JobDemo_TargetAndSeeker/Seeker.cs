using UnityEngine;

public class Seeker : MonoBehaviour
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

    private void LateUpdate()
    {
        if(TargetAndSeekerManager.targetsTransform != null)
        {
            Transform nearestTarget = FindNearestTarget(TargetAndSeekerManager.targetsTransform);
            Debug.DrawLine(transform.position, nearestTarget.position, Color.white);
        }
    }

    private Transform FindNearestTarget(Transform[] targetTransforms)
    {
        Transform nearestTarget = null;
        foreach (Transform t in targetTransforms)
        {
            if (nearestTarget == null)
            {
                nearestTarget = t;
            }
            else
            {
                float distWithTarget = Vector3.Distance(transform.position, t.position);
                float distWithNearest = Vector3.Distance(transform.position, nearestTarget.position);

                if (distWithTarget < distWithNearest)
                {
                    nearestTarget = t;
                }
            }     
        }

        return nearestTarget;
    }
}
