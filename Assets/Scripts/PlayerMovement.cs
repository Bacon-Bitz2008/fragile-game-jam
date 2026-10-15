using JetBrains.Annotations;
using System;
using System.Collections;
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
    [SerializeField] private float shieldDuration = 0.3f;
    [SerializeField] private GameObject shield;
    private Coroutine shieldRoutine;
    private bool isRunning = false;
    public bool getP1() { return isP1; }
    private Ball lastThrownBall;
    private CatchHitbox catchHitbox;
    private int ballCount;
    public int getBallCount() { return ballCount; }
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

    [Header("Dash Particles")]
    [SerializeField] private ParticleSystem dashParticles;      // optional: leave empty and one gets made automatically
    [SerializeField] private Material dashParticleMaterial;     // optional: drag a material in for custom looks (recommended for builds)
    [SerializeField] private Color dashColor = Color.white;
    [SerializeField] private float dashParticleHeight = 0.5f;   // how high off the player's pivot the trail spawns

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

        SetupDashParticles();
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





            
            bool wasIdle = prevInput.sqrMagnitude < 0.25f;
            bool nowActive = walkInput.sqrMagnitude >= 0.25f;

        
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

                    StartDashParticles();
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

                    StopDashParticles();
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




    
    private void SetupDashParticles()
    {
        if (dashParticles != null)
        {
            // one was assigned in the Inspector, just make sure it doesn't play on its own
            var assignedMain = dashParticles.main;
            assignedMain.playOnAwake = false;
            assignedMain.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
            dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }

        GameObject go = new GameObject("DashParticles");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, dashParticleHeight, 0f);
        go.transform.localRotation = Quaternion.identity;

        dashParticles = go.AddComponent<ParticleSystem>();

        // a new particle system starts playing right away, and some settings can't change while playing
        dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = dashParticles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = 0.35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startColor = dashColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // particles stay behind = trail
        main.maxParticles = 200;
        // measure movement from the transform, not the Rigidbody (we move with transform.Translate)
        main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

        // only spawn particles while actually moving
        var emission = dashParticles.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 15f;

        var shape = dashParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // fade out
        var colorOverLifetime = dashParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[] { new GradientColorKey(dashColor, 0f), new GradientColorKey(dashColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        // shrink out
        var sizeOverLifetime = dashParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // material (URP needs a URP particle shader or it shows up pink)
        ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
        if (dashParticleMaterial != null)
        {
            psRenderer.material = dashParticleMaterial;
        }
        else
        {
            Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticle != null)
            {
                psRenderer.material = new Material(urpParticle);
            }
        }
    }

    private void StartDashParticles()
    {
        if (dashParticles != null) dashParticles.Play();
    }

    private void StopDashParticles()
    {
        // StopEmitting lets the existing particles fade out instead of vanishing
        if (dashParticles != null) dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }




    public void Catch_Performed(InputAction.CallbackContext context)
    {
        if (catchCoolDown <= 0)
        {
            if (ballCount < 2)
            {


                catchCoolDown += 5;
                ActivateShield();
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

    public void ActivateShield()
    {
        if (shieldRoutine != null) StopCoroutine(shieldRoutine);
        shieldRoutine = StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        shield.SetActive(true);
        yield return new WaitForSeconds(shieldDuration);
        shield.SetActive(false);
        shieldRoutine = null;
    }



    public void Shoot_performed(InputAction.CallbackContext context)
    {
        if (ballCount > 0 && !gameSys.isGamePaused())
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
        if (stat.CanTakeDamage())
        {
            print("oof");
            stat.Health--;
        }
        
    }
}