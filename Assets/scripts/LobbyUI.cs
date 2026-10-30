using UnityEngine;
using TMPro;

// Mostra el best score a la UI del Lobby.
// Posa aquest script a qualsevol GameObject de la LobbyScene (per exemple a [MANAGERS])
// i arrossega el text TMP on s'ha de veure el best score.
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

        // Mateixa clau de PlayerPrefs que fa servir el ScoreManager de la GameScene
        bestScoreText.text = prefix + ScoreManager.GetHighScore();
    }
}
