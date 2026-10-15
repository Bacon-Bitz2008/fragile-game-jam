
using JetBrains.Annotations;
using System;
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

    [SerializeField] private GameSystem gameSys;

    [SerializeField] private Animator Ani;

    private bool isRunning = false;

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

    //Ani.Play("Player Run");
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
        



       


        if (!gameSys.isGamePaused())
        {
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

            // fixing the animation on player

            bool movingNow = walkVector != Vector3.zero;

            if (movingNow)
            {
                transform.rotation = Quaternion.LookRotation(walkVector);
            }

            // only change animation when the state actually changes
            if (movingNow && !isRunning)
            {
                Ani.Play("Player Run");
                isRunning = true;
            }
            else if (!movingNow && isRunning)
            {
                Ani.Play("Player Idle");
                isRunning = false;
            }
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
        if(ballCount > 0 && !gameSys.isGamePaused())
        {
            AudioSource aS = GetComponent<AudioSource>();
            aS.Play();

            GameObject newBall = Instantiate(ball);
            lastThrownBall = newBall.GetComponent<Ball>();
            lastThrownBall.setThrownFrom(gameObject.name);
            newBall.transform.position = transform.position;
            Rigidbody brb = newBall.GetComponent<Rigidbody>();

            brb.AddForce(transform.forward * launchSpeed, ForceMode.Impulse);
            brb.AddForce(transform.up * launchSpeed * 0.15f, ForceMode.Impulse);

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
