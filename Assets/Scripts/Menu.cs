using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    [SerializeField] private GameObject howToPanel;


    public void goStart()
    {
        SceneManager.LoadScene("Main");
    }


    public void goQuit()
    {
        Application.Quit();
    }

    public void goHowTo()
    {
        howToPanel.SetActive(true);
    }

    public void quitHowTo()
    {
        howToPanel.SetActive(false);
    }

}
