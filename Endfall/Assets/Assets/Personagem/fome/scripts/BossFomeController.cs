using UnityEngine;

public class BossFomeController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private GameObject prefabFlecha;

    [Header("Configurações de Ataque")]
    [SerializeField] private float alcanceAtaque = 15f;
    [SerializeField] private float tempoEntreAtaques = 3f;

    private float cronometroAtaque;
    private bool olhandoDireita = false;

    private void Start()
    {
        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        OlharParaJogador();

        cronometroAtaque += Time.deltaTime;

        if (distancia <= alcanceAtaque && cronometroAtaque >= tempoEntreAtaques)
        {
            Atacar();
            cronometroAtaque = 0f;
        }
    }

    private void OlharParaJogador()
    {
        if (player.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (player.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }
    }

    private void Virar()
    {
        olhandoDireita = !olhandoDireita;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    private void Atacar()
    {
        if (animator != null)
        {
            animator.SetTrigger("Atacar");
        }
    }

    public void DispararFlecha()
    {
        if (prefabFlecha != null && pontoDisparo != null && player != null)
        {
            GameObject flechaObj = Instantiate(prefabFlecha, pontoDisparo.position, Quaternion.identity);
            FlechaBoss flecha = flechaObj.GetComponent<FlechaBoss>();

            if (flecha != null)
            {
                Vector2 direcao = (player.position - pontoDisparo.position).normalized;
                flecha.DefinirDirecao(direcao);
            }
        }
    }
}