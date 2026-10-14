using System;
using TMPro;
using UnityEngine;

public class GameSystem : MonoBehaviour
{

    private int p1p;
    private int p2p;

    [SerializeField] private TextMeshProUGUI p1txt;
    [SerializeField] private TextMeshProUGUI p2txt;

    [SerializeField] private TextMeshProUGUI p1h;
    [SerializeField] private TextMeshProUGUI p2h;

    [SerializeField] private PlayerStatus p1;
    [SerializeField] private PlayerStatus p2;


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

        p1txt.text = "LEFT PLAYER    " + p1p;
        p2txt.text = p2p + "    RIGHT PLAYER";

        p1h.text = "HEALTH    " + p1.Health;
        p2h.text = p2.Health + "    HEALTH";
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
