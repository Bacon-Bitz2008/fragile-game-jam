using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStatus : MonoBehaviour
{


    [SerializeField] private GameSystem gameSys;
    [SerializeField] private PlayerMovement pm;


    private int maxHealth = 3;


    [Tooltip("Assign CrackShatter3D here, or leave empty to find it automatically.")]
    [SerializeField] private CrackShatter3D crackEffect;


    private int health;
    private bool dead;

    private int playerID;

    //public int MaxHealth => maxHealth;
    private void Start()
    {
        controls.Health.Enable();

    }
    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0, maxHealth);

            if (crackEffect != null)
            {
                crackEffect.SetHealth(health, maxHealth);
                Debug.Log($"Health: {health}/{maxHealth}");
            }
            else
            {
                Debug.LogWarning("CrackShatter3D is not assigned or found!");
            }
        }
    }

    private void Awake()
    {
<<<<<<< HEAD
       
=======

        if (pm.getP1())
        {
            playerID = 0;
        }
        else
        {
            playerID = 1;
        }
>>>>>>> 39a02f0e116e8486dfd86f971717403f172d23e4

        // Find the crack effect first.
        if (crackEffect == null)
            crackEffect = GetComponentInChildren<CrackShatter3D>();

        // Initialize health through the property.
        health = maxHealth;
        Health = maxHealth;

<<<<<<< HEAD

        controls.Health.Enable();
        controls = new Controls();
        controls.Health.Damage.performed += Damage_performed;
       
=======
>>>>>>> 39a02f0e116e8486dfd86f971717403f172d23e4
    }

    private void Update()
    {

<<<<<<< HEAD
        Debug.Log("damage taken");

        
        Health -= 25;

        if (health <= 0 && !dead)
            Die();
=======
        if(health <= 0)
        {
            gameSys.updateScore(playerID);
            health = maxHealth;
            crackEffect.ResetCracks();
        }

>>>>>>> 39a02f0e116e8486dfd86f971717403f172d23e4
    }



    private void Die()
    {
        dead = true;
        Debug.Log("Dead!");
    }


}
