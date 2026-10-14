using TMPro;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{

    [SerializeField] private GameSystem gameSys;
    [SerializeField] private PlayerMovement pm;


    private int maxHealth = 3;


    private int health;

    private int playerID;

    public int Health
    {
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
        {
            die();
        }
    }

    private void die()
    {
        print("dead");
        gameSys.updateScore(playerID);
        health = maxHealth;
    }
}
