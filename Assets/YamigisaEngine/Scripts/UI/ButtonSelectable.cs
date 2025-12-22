using UnityEngine.UI;
using UnityEngine;

public class ButtonInteractiveObject : MonoBehaviour
{
    public Button Button;
    [SerializeField] private Text label;

    public void SetText(string newText)
    {
        if (label) label.text = newText;
    }
}
