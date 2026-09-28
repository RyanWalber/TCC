using UnityEngine;
using TMPro; 

public class HUDManager : MonoBehaviour
{
    [Header("UI Referências")]
    [SerializeField] private TextMeshProUGUI textoMoedas; 

    private void Update()
    {
        if (textoMoedas != null && LevelManager.Instance != null)
        {
            textoMoedas.text = LevelManager.Instance.moedasFaseAtual.ToString();
        }
    }
}