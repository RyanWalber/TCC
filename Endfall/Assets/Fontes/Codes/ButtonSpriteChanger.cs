using UnityEngine;
using UnityEngine.UI;

public class ButtonSpriteChanger : MonoBehaviour
{
    public Button[] buttons;
    public Sprite activeSprite;

    private Sprite[] originalSprites;

    private void Start()
    {
        originalSprites = new Sprite[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            originalSprites[i] = buttons[i].GetComponent<Image>().sprite;
        }

        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => OnButtonClick(button));
        }
    }

    private void OnButtonClick(Button clickedButton)
    {
        Image clickedButtonImage = clickedButton.GetComponent<Image>();
        clickedButtonImage.sprite = activeSprite;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != clickedButton)
            {
                buttons[i].GetComponent<Image>().sprite = originalSprites[i];
            }
        }
    }
}
