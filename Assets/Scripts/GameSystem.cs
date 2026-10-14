using UnityEngine;

public class GameSystem : MonoBehaviour
{

    private int p1p;
    private int p2p;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(p1p >= 5 || p2p >= 5)
        {
            endGame();
        }
    }


    public void updateScore(int playerID)
    {
        if(playerID == 0)
        {
            p2p++;
        }
        else if (playerID == 1)
        {
            p1p++;
        }
        else
        {
            print("ERROR: invalid playerID.");
        }
    }

    private void endGame()
    {
        print("GAME OVER");
    }
}
