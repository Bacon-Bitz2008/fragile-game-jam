using UnityEngine;

public class PlayerStatus : MonoBehaviour
{

    private int health;

    public int Health
    {
        get { return health; } 
        set { health = value; }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
    }
}
