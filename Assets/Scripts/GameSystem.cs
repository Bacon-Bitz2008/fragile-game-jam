using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [SerializeField] private TextMeshProUGUI winTxt;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject winButtons;

    [SerializeField] private GameObject pauseScreen;

    private bool gamePaused;

    private bool gameEnded;
    public bool isGamePaused() { return gamePaused; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(p1p >= 5 || p2p >= 5)
        {
            if ((!gameEnded))
            {
                endGame();
            }
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
        winScreen.SetActive(true);
        if (p1p >= 5 && p2p < 5)
        {
            winTxt.text = "LEFT PLAYER WINS";
        }
        else if (p2p >= 5 && p1p < 5)
        {
            winTxt.text = "RIGHT PLAYER WINS";
        }
        else
        {
            winTxt.text = "DRAW";
        }


        Invoke("delayToggle", 2);
        gameEnded = true;
    }

    private void delayToggle()
    {
        winButtons.SetActive(true);

    }

    public void restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void goMenu()
    {
        SceneManager.LoadScene(0);
    }


    public void togglePause()
    {
        gamePaused = !gamePaused;

        pauseScreen.SetActive(gamePaused);
    }
}
