using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private int numberToSpawn;
    [SerializeField] private GameObject enemyToSpawnWithPhysics;
    [SerializeField] private GameObject enemyToSpawnNoPhysics;
    [SerializeField] private float speed;
    [SerializeField] private float tooCloseDistance;
    [SerializeField] private float xAreaLimit;
    [SerializeField] private float yAreaLimit;

    [SerializeField] private bool useManagerForEnemyBehavior;
    [SerializeField] private bool useJobs;
    [SerializeField] private bool enablePhysics;

    private List<GameObject> enemies = new List<GameObject>();
    private TransformAccessArray tfAccessArray;
    private Transform[] enemiesTransform;
    private Transform playerTransform;
    private bool awakeDone = false;

    //private JobHandle enemiesJobHandle;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        enemiesTransform = new Transform[numberToSpawn];

        // Spawns enemies
        for (int i = 0; i < numberToSpawn; i++)
        {
            float randomX = Random.Range(-xAreaLimit, xAreaLimit);
            float randomY = Random.Range(-yAreaLimit, yAreaLimit);
            Vector3 randomPosition = new Vector3(randomX, randomY, 0);
            GameObject newEnemy = Instantiate( enablePhysics ? enemyToSpawnWithPhysics : enemyToSpawnNoPhysics, randomPosition, Quaternion.identity);
            enemies.Add(newEnemy);
            enemiesTransform[i] = newEnemy.transform;

            var behavior = newEnemy.GetComponent<EnemyBehavior>();
            if (useManagerForEnemyBehavior)
            {
                behavior.enabled = false;
            }
            else
            {
                behavior.enabled = true;
                behavior.useJobs = useJobs;
            }

        }

        tfAccessArray = new TransformAccessArray(enemiesTransform);

        awakeDone = true;
    }

    void Update()
    {
        if(awakeDone)
        {
            if (useManagerForEnemyBehavior == false) return;
            else
            {
                if (useJobs)
                {
                    EnemiesBehaviorJob job = new EnemiesBehaviorJob()
                    {
                        PlayerPosition = playerTransform.position,
                        Speed = speed,
                        TooCloseDistance = tooCloseDistance,
                        XAreaLimit = xAreaLimit,
                        YAreaLimit = yAreaLimit,
                        DeltaTime = Time.deltaTime
                    };

                    //enemiesJobHandle = job.Schedule(tfAccessArray);
                    JobHandle handle = job.Schedule(tfAccessArray);
                    handle.Complete();
                }
                else
                {
                    foreach (GameObject enemy in enemies)
                    {
                        ProcessEnemyBehaviorNoJob(enemy);
                    }
                }
            }
        }
    }

    //private void LateUpdate()
    //{
    //    enemiesJobHandle.Complete();
    //}

    private void OnDestroy()
    {
        // Dispose TransformAccessArray
        tfAccessArray.Dispose();
    }

    //private void OnGUI()
    //{
    //    GUIStyle style = GUI.skin.GetStyle("textArea");
    //    style.fontSize = 16;
    //    GUI.Box(new Rect(10, 10, 250, 90), $" Items count: {numberToSpawn}\n Job enabled: {useJobs}\n Manager handle behavior: {useManagerForEnemyBehavior}\n Physics enabled: {enablePhysics}", style);
    //}

    private void ProcessEnemyBehaviorNoJob(GameObject enemy) // Remove Monobehavior overhead by calculating everything in one single manager behavior
    {
        Vector3 dirPlayerToEnemy = (enemy.transform.position - playerTransform.position).normalized;
        bool playerLowerThanEnemy = playerTransform.position.y < enemy.transform.position.y;
        bool playerMoreLeftThanEnemy = playerTransform.position.x < enemy.transform.position.x;

        if (Vector3.Distance(enemy.transform.position, playerTransform.position) < tooCloseDistance)
        {
            Vector3 positionPrevision = enemy.transform.position + (dirPlayerToEnemy / 2);
            Vector3 direction;

            if (positionPrevision.x >= xAreaLimit) // Reach limit on the right
            {
                if (playerLowerThanEnemy) // Player is lower so we need to go counter clockwise
                {
                    direction = GetPerpendicularCounterClockwiseVector(dirPlayerToEnemy);
                }
                else // Player is higher so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
            }
            else if (positionPrevision.x <= -xAreaLimit) // Reach limit on the left
            {
                if (playerLowerThanEnemy) // Player is lower so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
                else // Player is higher so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
            }
            else if (positionPrevision.y >= yAreaLimit) // Reach limit on the top
            {
                if (playerMoreLeftThanEnemy) // Player is at our left so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
                else // Player is at our right so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
            }
            else if (positionPrevision.y <= -yAreaLimit) // Reach limit on the bottom
            {
                if (playerMoreLeftThanEnemy) // Player is at our left so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
                else // Player is at our right so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToEnemy);
                }
            }
            else
            {
                direction = dirPlayerToEnemy;
            }

            Vector3 nextPosition = enemy.transform.position + (direction * speed * Time.deltaTime);
            enemy.transform.position = nextPosition;
        }
        //else if (Vector3.Distance(enemy.transform.position, playerTransform.position) > 8f)
        //{
        //    Vector3 dirEnemyToPlayer = (playerTransform.position - enemy.transform.position).normalized;
        //    Vector3 nextPosition = enemy.transform.position + (dirEnemyToPlayer * speed * Time.deltaTime);
        //    enemy.transform.position = nextPosition;
        //}
    }

    private Vector3 GetPerpendicularClockwiseVector(Vector3 initialVector)
    {
        return new Vector3(initialVector.y, -initialVector.x, initialVector.z);
    }

    private Vector3 GetPerpendicularCounterClockwiseVector(Vector3 initialVector)
    {
        return new Vector3(-initialVector.y, initialVector.x, initialVector.z);
    }
}
