using UnityEngine;

public class Ball : MonoBehaviour
{

    [SerializeField] private bool dead;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 7)
        {
            dead = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 6)
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();

            if (dead && (pm.getBallCount() < pm.getMaxBalls()))
            {
                pm.collectBall();
                Destroy(gameObject);
            }
            else if (!dead)
            {
                pm.takeHit();
            }
        }
    }
}
