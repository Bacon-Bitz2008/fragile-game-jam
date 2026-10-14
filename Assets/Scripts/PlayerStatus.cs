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
<<<<<<< HEAD
  
=======
    private void Start()
    {


    }
>>>>>>> 72f5d68d0b1e58cc45f4638a865ea91fd56ed5fe
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


<<<<<<< HEAD

      
       

=======
       
>>>>>>> 72f5d68d0b1e58cc45f4638a865ea91fd56ed5fe
    }

    private void Update()
    {

<<<<<<< HEAD

        Debug.Log("damage taken");

        
       

        if (health <= 0 && !dead)
            Die();

=======

>>>>>>> 72f5d68d0b1e58cc45f4638a865ea91fd56ed5fe
        if(health <= 0)
        {
            gameSys.updateScore(playerID);
            health = maxHealth;
            crackEffect.ResetCracks();
        }


    }



    private void Die()
    {
        dead = true;
        Debug.Log("Dead!");
    }


}
