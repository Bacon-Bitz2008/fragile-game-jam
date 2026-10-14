
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

<<<<<<< HEAD
    [Tooltip("Assign CrackShatter3D here, or leave empty to find it automatically.")]
    [SerializeField] private CrackShatter3D crackEffect;

    private Controls controls;
=======
    [SerializeField] private GameSystem gameSys;
    [SerializeField] private PlayerMovement pm;

    private int maxHealth = 3;


>>>>>>> c9cf3e1f2695b0251339ca7a428575339298a2a8
    private int health;
    private bool dead;

    public int MaxHealth => maxHealth;

    private int playerID;

    public int Health
    {
<<<<<<< HEAD
        get => health;
        set
=======
        get { return health; } 
        set { health = value; }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        health = maxHealth;

        if (pm.getP1())
        {
            playerID = 0;
        }
        else
        {
            playerID = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
>>>>>>> c9cf3e1f2695b0251339ca7a428575339298a2a8
        {
            health = Mathf.Clamp(value, 0, maxHealth);

            if (crackEffect != null)
            {
                crackEffect.SetHealth(health, maxHealth);
                Debug.Log($"Health: {health}/{maxHealth} - Crack effect updated");
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
        // Find the crack effect first.
        if (crackEffect == null)
            crackEffect = GetComponentInChildren<CrackShatter3D>();

        // Initialize health through the property.
        health = maxHealth;
        Health = maxHealth;

        controls = new Controls();
        controls.Health.Damage.performed += Damage_performed;
        controls.Health.Enable();
    }

    private void Damage_performed(InputAction.CallbackContext context)
    {
        if (dead) return;

        

        // IMPORTANT: Use the property, not health -= 10.
        Health -= 10;

        if (health <= 0 && !dead)
            Die();
    }

    private void Die()
    {
        dead = true;
        Debug.Log("Dead!");
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Health.Damage.performed -= Damage_performed;
            controls.Dispose();
        }
=======
        print("dead");
        gameSys.updateScore(playerID);
        health = maxHealth;
>>>>>>> c9cf3e1f2695b0251339ca7a428575339298a2a8
    }
}
