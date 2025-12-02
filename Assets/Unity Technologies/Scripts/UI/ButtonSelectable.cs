using UnityEngine.UI;
using UnityEngine;

public class ButtonSelectable : MonoBehaviour
{
    public Button Button;
    [SerializeField] private Text label;

    public void SetText(string newText)
    {
        if (label) label.text = newText;
    }
}
