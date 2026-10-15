using UnityEngine;
using UnityEngine.Audio;

public class Ball : MonoBehaviour
{

    [SerializeField] private bool dead;

    [SerializeField] private Collider coll;

    private AudioSource audSrc;

    private string thrownFrom;
    public void setThrownFrom(string thrownFrom)
    {
        this.thrownFrom = thrownFrom;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audSrc = GetComponent<AudioSource>();
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

    public bool isDead
    {
        get { return dead; }
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
                audSrc.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        coll.excludeLayers = 0;
    }

    public void BallCaught()
    {
        Destroy(gameObject);
    }
}
