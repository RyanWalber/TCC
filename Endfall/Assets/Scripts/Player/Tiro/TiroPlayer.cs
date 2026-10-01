using UnityEngine;

public class TiroPlayer : MonoBehaviour
{
    [Header("Configurações do Tiro")]
    public GameObject prefabProjetil;
    public GameObject prefabEfeitoFogo;
    public Transform pontoDeDisparo;
    public KeyCode teclaTiro = KeyCode.J;
    public float cadenciaTiro = 0.2f;

    private float tempoProximoTiro;
    private SistemaFuria sistemaFuria;

    private void Start()
    {
        sistemaFuria = GetComponentInParent<SistemaFuria>();
    }

    private void Update()
    {
        float cadenciaEfetiva = cadenciaTiro;

        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
        {
            cadenciaEfetiva *= sistemaFuria.multCadencia;
        }

        if ((Input.GetKeyDown(teclaTiro) || Input.GetMouseButtonDown(0)) && Time.time >= tempoProximoTiro)
        {
            Atirar();
            tempoProximoTiro = Time.time + cadenciaEfetiva;
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