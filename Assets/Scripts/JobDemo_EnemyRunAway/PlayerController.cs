using UnityEngine;

namespace Jobs.EnemyRunAwayDemo
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed;

        [SerializeField] private float xAreaLimit;
        [SerializeField] private float yAreaLimit;

        void Update()
        {
            if (Input.GetKey(KeyCode.UpArrow) && transform.position.y < yAreaLimit)
            {
                transform.position = transform.position + (transform.up * speed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.DownArrow) && transform.position.y > -yAreaLimit)
            {
                transform.position = transform.position + (-transform.up * speed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.LeftArrow) && transform.position.x > -xAreaLimit)
            {
                transform.position = transform.position + (-transform.right * speed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.RightArrow) && transform.position.x < xAreaLimit)
            {
                transform.position = transform.position + (transform.right * speed * Time.deltaTime);
            }
        }
    } 
}
