using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float tooCloseDistance;
    [SerializeField] private float xAreaLimit;
    [SerializeField] private float yAreaLimit;


    [SerializeField] private bool showDebugObj;

    [SerializeField] private GameObject positionPrevisionObj;
    [SerializeField] private GameObject dirObj;

    private Transform playerTransform;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if(showDebugObj)
        {
            positionPrevisionObj.SetActive(true);
            dirObj.SetActive(true);
        }
        else
        {
            positionPrevisionObj.SetActive(false);
            dirObj.SetActive(false);
        }
    }

    void Update()
    {
        Vector3 dirPlayerToThis = (transform.position - playerTransform.position).normalized;
        bool playerLowerThanThis = playerTransform.position.y < transform.position.y;
        bool playerMoreLeftThanThis = playerTransform.position.x < transform.position.x;

        if (Vector3.Distance(transform.position, playerTransform.position) < tooCloseDistance)
        {
            Vector3 positionPrevision = transform.position + (dirPlayerToThis / 2);
            positionPrevisionObj.transform.position = positionPrevision;

            Debug.Log(positionPrevision);

            Vector3 direction;
            ;

            if (positionPrevision.x >= xAreaLimit) // Reach limit on the right
            {
                if (playerLowerThanThis) // Player is lower so we need to go counter clockwise
                {
                    direction = GetPerpendicularCounterClockwiseVector(dirPlayerToThis);  
                }
                else // Player is higher so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
            }
            else if (positionPrevision.x <= -xAreaLimit) // Reach limit on the left
            {
                if (playerLowerThanThis) // Player is lower so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
                else // Player is higher so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
            }
            else if(positionPrevision.y >= yAreaLimit) // Reach limit on the top
            {
                if (playerMoreLeftThanThis) // Player is at our left so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
                else // Player is at our right so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
            }
            else if (positionPrevision.y <= -yAreaLimit) // Reach limit on the bottom
            {
                if (playerMoreLeftThanThis) // Player is at our left so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
                else // Player is at our right so we need to go clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
            }
            else
            {
                direction = dirPlayerToThis;
            }

            dirObj.transform.position = transform.position + (direction/2);
            Vector3 nextPosition = transform.position + (direction * speed * Time.deltaTime);
            transform.position = nextPosition;
        }
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
