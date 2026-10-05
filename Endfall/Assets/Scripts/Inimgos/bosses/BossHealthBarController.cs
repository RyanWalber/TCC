using UnityEngine;

public class BossHealthBarController : MonoBehaviour
{
    [Header("Configurações da UI")]
    public GameObject barraVidaUI;       // Arrasta o objeto BarraVidaBoss para aqui
    public Transform jogador;            // Pode deixar vazio no Inspector!
    public float distanciaAtivacao = 10f; // Distância individual deste boss

    void Start()
    {
        BuscarJogador();

        // Garante que a barra começa escondida
        if (barraVidaUI != null)
        {
            barraVidaUI.SetActive(false);
        }
    }

    void Update()
    {
        if (barraVidaUI == null) return;

        // 1. Se o jogo pausou ou deu Game Over (Time.timeScale perto de 0)
        if (Time.timeScale <= 0.01f)
        {
            barraVidaUI.SetActive(false);
            return;
        }

        // Se perdeu a referência do jogador, tenta encontrar
        if (jogador == null)
        {
            BuscarJogador();

            // Se mesmo assim não encontrou, esconde a barra
            if (jogador == null)
            {
                barraVidaUI.SetActive(false);
                return;
            }
        }

        // 2. Se a Kaya foi desativada no Game Over (GameObject invisível/desativado)
        if (!jogador.gameObject.activeInHierarchy)
        {
            barraVidaUI.SetActive(false);
            return;
        }

        // 3. Calcula a distância se o jogador estiver vivo e ativo na cena
        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia <= distanciaAtivacao)
        {
            barraVidaUI.SetActive(true);
        }
        else
        {
            barraVidaUI.SetActive(false);
        }
    }

    private void BuscarJogador()
    {
        if (jogador == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) jogador = p.transform;
        }
    }
}