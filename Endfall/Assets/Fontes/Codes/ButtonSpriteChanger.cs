using UnityEngine;
using UnityEngine.UI;

public class ButtonSpriteChanger : MonoBehaviour
{
    public Button[] buttons; // Array untuk semua tombol yang akan diatur.
    public Sprite activeSprite; // Sprite yang digunakan ketika tombol diklik.

    private Sprite[] originalSprites; // Array untuk menyimpan sprite awal dari masing-masing tombol.

    private void Start()
    {
        // Simpan sprite awal dari masing-masing tombol.
        originalSprites = new Sprite[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            originalSprites[i] = buttons[i].GetComponent<Image>().sprite;
        }

        // Assign listener onClick untuk setiap tombol.
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => OnButtonClick(button));
        }
    }

    private void OnButtonClick(Button clickedButton)
    {
        // Ubah sprite dari tombol yang diklik.
        Image clickedButtonImage = clickedButton.GetComponent<Image>();
        clickedButtonImage.sprite = activeSprite;

        // Kembalikan sprite semua tombol lainnya ke sprite awal masing-masing.
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != clickedButton)
            {
                buttons[i].GetComponent<Image>().sprite = originalSprites[i];
            }
        }
    }
}
