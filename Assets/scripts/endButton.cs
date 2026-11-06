using UnityEngine;


public class endButton : MonoBehaviour
{
    bool used;

    // El crida pistolaScript quan el raig toca el botó
    public void hitted()
    {
        if (used) return; // evita carregar l'escena dues vegades
        used = true;
        goToMenu();
    }

    public void goToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}
