using UnityEngine;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Tooltip("Text TMP (World Space) on es mostra el best score")]
    public TMP_Text bestScoreText;
    [Tooltip("Text que va davant del número")]
    public string prefix = "Best score: ";

    void Start()
    {
        if (bestScoreText == null)
        {
            Debug.LogWarning("[LobbyUI] Falta assignar el text del best score a l'Inspector", this);
            return;
        }

        bestScoreText.text = prefix + ScoreManager.GetHighScore();
    }
}
