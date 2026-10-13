using UnityEngine;

public class Ball : MonoBehaviour
{

    [SerializeField] private bool dead;

    [SerializeField] private Collider coll;

    private string thrownFrom;
    public void setThrownFrom(string thrownFrom)
    {
        this.thrownFrom = thrownFrom;
    }

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
            else if (!dead && (other.gameObject.name != thrownFrom))
            {
                pm.takeHit();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        coll.excludeLayers = 0;
    }
}
