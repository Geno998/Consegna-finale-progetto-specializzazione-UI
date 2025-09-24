using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RequirementRow : MonoBehaviour
{
    // Riferimenti UI per icona, nome e quantità richiesta
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;

    // Imposta i contenuti della riga (sprite, nome, testo quantità) e colore in base alla disponibilità
    public void Set(Sprite sprite, string displayName, string amount, bool enough)
    {
        if (icon) icon.sprite = sprite;
        if (nameText) nameText.text = displayName;
        if (amountText)
        {
            amountText.text = amount;
            amountText.color = enough ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.7f, 0.7f);
        }
    }
}
