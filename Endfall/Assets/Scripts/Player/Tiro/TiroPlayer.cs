using UnityEngine;

public class TiroPlayer : MonoBehaviour
{
    public GameObject prefabProjetil;
    public GameObject prefabEfeitoFogo;
    public Transform pontoDeDisparo;
    public KeyCode teclaTiro = KeyCode.J;
    public float cadenciaTiro = 0.2f;

    private float tempoProximoTiro;

    private void Update()
    {
        if ((Input.GetKeyDown(teclaTiro) || Input.GetMouseButtonDown(0)) && Time.time >= tempoProximoTiro)
        {
            Atirar();
            tempoProximoTiro = Time.time + cadenciaTiro;
        }
    }

    private void Atirar()
    {
        if (prefabProjetil == null || pontoDeDisparo == null) return;

        GameObject tiro = Instantiate(prefabProjetil, pontoDeDisparo.position, pontoDeDisparo.rotation);

        Projetil scriptProjetil = tiro.GetComponent<Projetil>();
        if (scriptProjetil != null)
        {
            scriptProjetil.Disparar();
        }

        if (prefabEfeitoFogo != null)
        {
            Instantiate(prefabEfeitoFogo, pontoDeDisparo.position, pontoDeDisparo.rotation, pontoDeDisparo);
        }
    }
}