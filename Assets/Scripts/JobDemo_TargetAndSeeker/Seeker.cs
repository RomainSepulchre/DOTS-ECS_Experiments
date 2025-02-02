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
        // TODO: Use jobs for this
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
        // TODO: Seeker late update seems to take time even when not doing anything (~0.3ms)
        if (TargetAndSeekerManager.targetsTransform != null && TargetAndSeekerManager.instance != null)
        {
            if (TargetAndSeekerManager.instance.demoMode == TargetAndSeekerManager.DemoMode.MainThread)
            {
                Transform nearestTarget = FindNearestTarget(TargetAndSeekerManager.targetsTransform);
                Debug.DrawLine(transform.position, nearestTarget.position, Color.white);
            }
        }
    }

    private Transform FindNearestTarget(Transform[] targetTransforms)
    {
        Transform nearestTarget = null;
        float distWithNearestSq = float.MaxValue;
        foreach (Transform t in targetTransforms)
        {
            // Using squared distance is cheaper than distance when comparing 2 distance since it avoid computing a square root
            // I went to 5fps to 10 fps on the main thread solution with this change so when comparing distance on a large amount of items it has a real impact
            // Performance gain for the late update: 0.155-0.170ms -> 0.073-0.080ms
            float distWithTargetSq = Vector3.SqrMagnitude(t.position - transform.position);

            if (distWithTargetSq < distWithNearestSq)
            {
                nearestTarget = t;
                distWithNearestSq = distWithTargetSq;
            }
        }

        return nearestTarget;
    }
}
