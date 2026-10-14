using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;


    [SerializeField] private GameSystem gameSys;
    [SerializeField] private PlayerMovement pm;


    //private int maxHealth = 3;


    [Tooltip("Assign CrackShatter3D here, or leave empty to find it automatically.")]
    [SerializeField] private CrackShatter3D crackEffect;

    private Controls controls;
    private int health;
    private bool dead;

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
       

        // Find the crack effect first.
        if (crackEffect == null)
            crackEffect = GetComponentInChildren<CrackShatter3D>();

        // Initialize health through the property.
        health = maxHealth;
        Health = maxHealth;


        controls.Health.Enable();
        controls = new Controls();
        controls.Health.Damage.performed += Damage_performed;
       
    }

    private void Damage_performed(InputAction.CallbackContext context)
    {
        if (dead) return;

        Debug.Log("damage taken");

        
        Health -= 25;

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
    }
}
