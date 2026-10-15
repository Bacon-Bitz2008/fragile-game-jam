
using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.Audio;
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





    // all the stuff neede for the dash to work
    // Ik its a LOT
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private float doubleTapWindow = 0.25f;

    private Vector2 prevInput;
    private Vector2 lastTapDir;
    private float lastTapTime = -10f;
    private bool isDashing;
    private float dashTimeLeft;
    private float nextDashTime;
    private Vector3 dashDir;

    [SerializeField] private AudioResource shoot;
    [SerializeField] private AudioResource hold;



    [Header("Bounds")]
    [SerializeField] private Transform northBound;
    [SerializeField] private Transform southBound;
    [SerializeField] private Transform westBound;
    [SerializeField] private Transform eastBound;



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

        // prevent phasing thru walls hopefully

        if (isP1)
        {
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, westBound.position.x, southBound.position.x), transform.position.y, Mathf.Clamp(transform.position.z, southBound.position.z, northBound.position.z));
        }
        else
        {
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, southBound.position.x, eastBound.position.x), transform.position.y, Mathf.Clamp(transform.position.z, southBound.position.z, northBound.position.z));
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





            // --- detect a "tap": input going from nothing to something ---
            bool wasIdle = prevInput.sqrMagnitude < 0.25f;
            bool nowActive = walkInput.sqrMagnitude >= 0.25f;

            // this is to check for window of dashing in moving 
            if (wasIdle && nowActive)
            {
                Vector2 dir = walkInput.normalized;

                bool sameDirAsLastTap = Vector2.Dot(dir, lastTapDir) > 0.9f;
                bool withinWindow = Time.time - lastTapTime <= doubleTapWindow;

                if (withinWindow && sameDirAsLastTap && !isDashing && Time.time >= nextDashTime)
                {
                    // double tap -> dash
                    isDashing = true;
                    dashTimeLeft = dashDuration;
                    dashDir = new Vector3(dir.x, 0, dir.y);
                    lastTapTime = -10f; // reset so a third tap doesn't chain
                }
                else
                {
                    lastTapTime = Time.time;
                    lastTapDir = dir;
                }
            }
            prevInput = walkInput;






            // --- movement ---
            // with addition of dashing
            Vector3 walkVector = new Vector3(walkInput.x, 0, walkInput.y);
            bool movingNow;

            if (isDashing)
            {
                transform.Translate(dashDir * dashSpeed * Time.deltaTime, Space.World);
                transform.rotation = Quaternion.LookRotation(dashDir);

                dashTimeLeft -= Time.deltaTime;
                if (dashTimeLeft <= 0f)
                {
                    isDashing = false;
                    nextDashTime = Time.time + dashCooldown;
                }
                movingNow = true;
            }
            else
            {
                transform.Translate(walkVector * speed * Time.deltaTime, Space.World);

                movingNow = walkVector != Vector3.zero;
                if (movingNow)
                {
                    transform.rotation = Quaternion.LookRotation(walkVector);
                }
            }






            // --- animation ---
            // same from last time
            if (movingNow && !isRunning)
            {
                Ani.SetBool("Running", true);
                isRunning = true;
            }
            else if (!movingNow && isRunning)
            {
                Ani.SetBool("Running", false);
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
                    AudioSource aS = GetComponent<AudioSource>();
                    aS.resource = hold;
                    aS.Play();

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
            aS.resource = shoot;
            aS.Play();

            GameObject newBall = Instantiate(ball);
            lastThrownBall = newBall.GetComponent<Ball>();
            lastThrownBall.setThrownFrom(gameObject.name);
            newBall.transform.position = transform.position;
            Rigidbody brb = newBall.GetComponent<Rigidbody>();
            Ani.SetTrigger("Throw");
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
