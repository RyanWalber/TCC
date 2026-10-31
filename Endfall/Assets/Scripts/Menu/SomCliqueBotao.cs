using UnityEngine;
using UnityEngine.UI;

// Adicionado automaticamente aos botoes de menu, slots e pausa.
[RequireComponent(typeof(Button))]
public class SomCliqueBotao : MonoBehaviour
{
    private Button botao;
    private bool registrado;

    private void Awake()
    {
        Registrar();
    }

    public void Registrar()
    {
        if (registrado) return;
        botao = GetComponent<Button>();
        if (botao == null) return;
        botao.onClick.AddListener(Tocar);
        registrado = true;
    }

    private void Tocar()
    {
        if (AudioManagerGlobal.Instance != null)
            AudioManagerGlobal.Instance.TocarClique();
    }

    private void OnDestroy()
    {
        if (registrado && botao != null)
            botao.onClick.RemoveListener(Tocar);
    }
}
