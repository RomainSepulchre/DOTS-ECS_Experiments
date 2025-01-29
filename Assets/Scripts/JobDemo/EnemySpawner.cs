using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private int numberToSpawn;
    [SerializeField] private GameObject enemyToSpawn;
    [SerializeField] private float xAreaLimit;
    [SerializeField] private float yAreaLimit;

    [SerializeField] private List<GameObject> enemies;
    private Transform playerTransform;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Start()
    {
        for (int i = 0; i < numberToSpawn; i++)
        {
            float randomX = Random.Range(-xAreaLimit, xAreaLimit);
            float randomY = Random.Range(-yAreaLimit, yAreaLimit);
            Vector3 randomPosition = new Vector3(randomX, randomY, 0);
            GameObject newEnemy = Instantiate(enemyToSpawn, randomPosition, Quaternion.identity);
            enemies.Add(newEnemy);
        }
    }

    void Update()
    {
        
    }
}
