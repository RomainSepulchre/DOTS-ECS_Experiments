using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private int numberToSpawn;
    [SerializeField] private GameObject enemyToSpawn;
    [SerializeField] private float speed;
    [SerializeField] private float tooCloseDistance;
    [SerializeField] private float xAreaLimit;
    [SerializeField] private float yAreaLimit;

    [SerializeField] private bool useEnemyBehavior;

    private List<GameObject> enemies = new List<GameObject>();
    private Transform playerTransform;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Spawns enemies
        for (int i = 0; i < numberToSpawn; i++)
        {
            float randomX = Random.Range(-xAreaLimit, xAreaLimit);
            float randomY = Random.Range(-yAreaLimit, yAreaLimit);
            Vector3 randomPosition = new Vector3(randomX, randomY, 0);
            GameObject newEnemy = Instantiate(enemyToSpawn, randomPosition, Quaternion.identity);
            enemies.Add(newEnemy);

            if (useEnemyBehavior == false)
            {
                newEnemy.GetComponent<EnemyBehavior>().enabled = false;
            }
        }
    }

    void Update()
    {
        if (useEnemyBehavior) return;
        else
        {
            foreach (GameObject enemy in enemies)
            {
                ProcessEnemyBehaviorNoJob(enemy);
            }
        }
    }

    private void ProcessEnemyBehaviorNoJob(GameObject enemy) // Remove Monobehavior overhead by calculating everything in one single manager behavior
    {
        Vector3 dirPlayerToEnemy = (enemy.transform.position - playerTransform.position).normalized;
        bool playerLowerThanEnemy = playerTransform.position.y < enemy.transform.position.y;
        bool playerMoreLeftThanEnemy = playerTransform.position.x < enemy.transform.position.x;

        if (Vector3.Distance(enemy.transform.position, playerTransform.position) < tooCloseDistance)
        {
            Vector3 positionPrevision = enemy.transform.position + (dirPlayerToEnemy / 2);
            Vector3 direction;
            ;

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
