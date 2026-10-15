using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{

    // lots of the input system stuff came from last year's tutorial
    private Controls controls;
    private Rigidbody rb;

    [SerializeField] GameObject ball;
    [SerializeField] private float launchSpeed;

    [SerializeField] private float catchCoolDown;

    [SerializeField] private int speed;

    [SerializeField] private bool isP1;

    [SerializeField] private PlayerStatus stat;

    public bool getP1() { return isP1; }
    private Ball lastThrownBall;
    private CatchHitbox catchHitbox;
    private int ballCount;
    public int getBallCount() {  return ballCount; }
    private int maxBalls = 2;
    public int getMaxBalls()
    {
        return maxBalls;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controls = new Controls();
        catchHitbox = GetComponentInChildren<CatchHitbox>();
        if (isP1)
        {
            controls.Player1.Enable();
            controls.Player1.Shoot.performed += Shoot_performed;
            controls.Player1.Catch.performed += Catch_Performed;
        }
        else
        {
            controls.Player2.Enable();
            controls.Player2.Shoot.performed += Shoot_performed;
            controls.Player2.Catch.performed += Catch_Performed;
        }

        rb = GetComponent<Rigidbody>();

    }


    // Update is called once per frame
    void Update()
    {
      
        if (catchCoolDown >= 0)
        {
            catchCoolDown -= Time.deltaTime;
        }
        
        Vector2 walkInput;

        if (isP1)
        {
            walkInput = controls.Player1.Move.ReadValue<Vector2>();
        }
        else
        {
            walkInput = controls.Player2.Move.ReadValue<Vector2>();
        }

        Vector3 walkVector = new Vector3(walkInput.x, 0, walkInput.y);
        transform.Translate(walkVector * speed * Time.deltaTime, Space.World);

        if(walkVector != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(walkVector);

        }

        if (catchHitbox.isCanCatch == true)
        {
   
        }
   
    }

    public void Catch_Performed(InputAction.CallbackContext context)
    {
        if (catchCoolDown <= 0)
        {
            if (ballCount < 2)
            {
                catchCoolDown += 5;
                Ball incoming = catchHitbox.GetBallInRange();

                if (catchHitbox.isCanCatch == true)
                {
                    Debug.Log("caught");
                    ballCount++;
                    incoming.BallCaught();
                }
            }
        }
    }



    public void Shoot_performed(InputAction.CallbackContext context)
    {
        if(ballCount > 0)
        {
            GameObject newBall = Instantiate(ball);
            lastThrownBall = newBall.GetComponent<Ball>();
            lastThrownBall.setThrownFrom(gameObject.name);
            newBall.transform.position = transform.position;
            Rigidbody brb = newBall.GetComponent<Rigidbody>();

            brb.AddForce(transform.forward * launchSpeed, ForceMode.Impulse);
            brb.AddForce(transform.up * launchSpeed * 0.2f, ForceMode.Impulse);

            ballCount--;
            if (ballCount < 0)
            {
                ballCount = 0;
            }
        }
        
    }

    public void collectBall()
    {
        ballCount++;
    }

    public void takeHit()
    {
        print("oof");
        stat.Health--;
    }
}
