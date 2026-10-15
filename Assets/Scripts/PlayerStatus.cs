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


    private bool canTakeDamage = true;

    public bool CanTakeDamage() { return canTakeDamage; }


    //public int MaxHealth => maxHealth;

    private void Start()
    {


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

        if (pm.getP1())
        {
            playerID = 0;
        }
        else
        {
            playerID = 1;
        }


        // Find the crack effect first.
        if (crackEffect == null)
            crackEffect = GetComponentInChildren<CrackShatter3D>();

        // Initialize health through the property.
        health = maxHealth;
        Health = maxHealth;



    }

    private void Update()
    {




        

        
       

        if (health <= 0 && !dead)
            Die();


        if(health <= 0)

        if (!gameSys.isGamePaused())


        {

            if (health <= 0 && !dead)
                Die();

        }

    }



    private void Die()
    {
        canTakeDamage = false;
        gameSys.updateScore(playerID);
        health = maxHealth;
        dead = true;
        Debug.Log("Dead!");
        Invoke("enableDamage", 1);
    }


    private void enableDamage()
    {
        canTakeDamage = true;
        crackEffect.ResetCracks();
        dead = false;

    }


}
