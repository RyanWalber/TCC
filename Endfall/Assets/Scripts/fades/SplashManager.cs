using System.Collections;
using UnityEngine;

public class SplashManager : MonoBehaviour
{
    [SerializeField] private float tempoDeEspera = 1.5f; 
    [SerializeField] private string nomeCenaMenu = "Menu";

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(tempoDeEspera);

        LevelLoader.CarregarComFade(nomeCenaMenu);
    }
}