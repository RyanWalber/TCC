using UnityEngine;

public class TiroPlayerSimulado : MonoBehaviour
{
    [Header("CONFIGURAÇÃO DO TIRO")]
    public GameObject prefabProjetil;
    public Transform pontoDeDisparo;
    public KeyCode teclaTiro = KeyCode.J; // Ou Mouse0 para clique esquerdo

    private void Update()
    {
        if (Input.GetKeyDown(teclaTiro) || Input.GetMouseButtonDown(0))
        {
            Atirar();
        }
    }

    private void Atirar()
    {
        if (prefabProjetil == null) return;

        Vector3 posicaoSpawn = pontoDeDisparo != null ? pontoDeDisparo.position : transform.position;
        GameObject tiro = Instantiate(prefabProjetil, posicaoSpawn, Quaternion.identity);

        float direcaoPlayer = transform.localScale.x;

        ProjetilSimulado scriptProjetil = tiro.GetComponent<ProjetilSimulado>();
        if (scriptProjetil != null)
        {
            scriptProjetil.ConfigurarDirecao(direcaoPlayer);
        }
    }
}