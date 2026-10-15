using UnityEngine;

public class CatchHitbox : MonoBehaviour
{
    [SerializeField] private bool canCatch;
    private Ball ballInRange;
    public Ball GetBallInRange() {  return ballInRange; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public bool isCanCatch
    {
        get { return canCatch; }
    }
    
    
    
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.tag == "Ball")
        {
            if (other.isTrigger) return;

            Ball ball = other.GetComponent<Ball>();

            if (ball.isDead == false)
            {
                canCatch = true;
                ballInRange = ball;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.gameObject.tag == "Ball")
        {
            if (other.isTrigger) return;

            Ball ball = other.GetComponent<Ball>();

            if (ball.isDead == false)
            {
                canCatch = false;
                ballInRange = null;
            }
        }



    }
}
