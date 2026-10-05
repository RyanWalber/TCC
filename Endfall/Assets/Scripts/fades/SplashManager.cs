using System.Collections;
using UnityEngine;

public class SplashManager : MonoBehaviour
{
    [SerializeField] private float tempoDeEspera = 1.5f; // Tempo para exibir logo/marca (opcional)
    [SerializeField] private string nomeCenaMenu = "Menu";

    private IEnumerator Start()
    {
        // Aguarda um momento (útil se quiser mostrar a sua logo na tela)
        yield return new WaitForSeconds(tempoDeEspera);

        // Chama a transição com Fade para o Menu Principal
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.CarregarCena(nomeCenaMenu);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCenaMenu);
        }
    }
}