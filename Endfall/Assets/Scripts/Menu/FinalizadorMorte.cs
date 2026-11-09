using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Existe fora do objeto Boss, que e destruido apos sua morte.
public sealed class FinalizadorMorte : MonoBehaviour
{
    private const string CenaFinal = "VitoriaFinal";
    private static bool agendado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarAoAbrirPlay()
    {
        agendado = false;
    }

    public static void Agendar()
    {
        if (agendado || SceneManager.GetActiveScene().name != "FaseMorte")
            return;

        if (!Application.CanStreamedLevelBeLoaded(CenaFinal))
        {
            Debug.LogError("[EndFall] VitoriaFinal ausente da lista de cenas do Build.");
            return;
        }

        agendado = true;
        GameObject controlador = new GameObject("FinalizadorMorte");
        DontDestroyOnLoad(controlador);
        FinalizadorMorte componente = controlador.AddComponent<FinalizadorMorte>();
        componente.StartCoroutine(componente.AguardarMorte());
    }

    private IEnumerator AguardarMorte()
    {
        // Mostra brevemente o desfecho da luta antes do fade cinematografico.
        yield return new WaitForSecondsRealtime(2f);
        if (SceneManager.GetActiveScene().name == "FaseMorte")
            LevelLoader.CarregarComFade(CenaFinal);

        yield return new WaitForSecondsRealtime(3f);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        agendado = false;
    }
}
