using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VidaBossGuerra : MonoBehaviour
{
    [Header("CONFIGURAÇÃO DE VIDA")]
    [SerializeField] private float vidaMaxima = 500f;
    private float vidaAtual;

    [Header("FEEDBACK DE DANO")]
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float duracaoFlash = 0.15f;

    [Header("Elixires nas arenas dos chefes - opcional")]
    [Tooltip("Preencha com o prefab 'Vida' (HealthPickup). Vazio = nao gera nada.")]
    [SerializeField] private HealthPickup prefabElixirArena;
    [Tooltip("A Kaya precisa estar a esta distancia do chefe para o gerador iniciar.")]
    [Min(1f)] [SerializeField] private float distanciaAtivacaoElixires = 350f;
    [Tooltip("Tempo ate o primeiro elixir depois que Kaya entra na arena.")]
    [Min(0f)] [SerializeField] private float atrasoPrimeiroElixir = 7f;
    [Tooltip("Intervalo entre os elixires durante a luta.")]
    [Min(0.5f)] [SerializeField] private float intervaloSpawnElixires = 18f;
    [Tooltip("Maximo de elixires presentes ao mesmo tempo.")]
    [Min(1)] [SerializeField] private int maximoElixiresPresentes = 2;
    [Tooltip("Total permitido durante toda a luta. Use 0 para quantidade ilimitada.")]
    [Min(0)] [SerializeField] private int quantidadeTotalElixires = 6;
    [Tooltip("Quantidade de vida recuperada por elixir de arena.")]
    [Min(1f)] [SerializeField] private float curaElixirArena = 25f;
    [Tooltip("Opcional: arraste pontos definidos na Hierarchy. Quando preenchidos, o gerador escolhe dentre eles.")]
    [SerializeField] private Transform[] pontosElixires;
    [Header("Elixires - surgimento automatico sem pontos")]
    [Tooltip("Tenta encontrar plataformas embaixo desta area horizontal ao redor do chefe.")]
    [Min(1f)] [SerializeField] private float raioSpawnElixires = 200f;
    [SerializeField] private LayerMask chaoParaElixires = 1 << 3;
    [Min(0f)] [SerializeField] private float alturaAcimaDoChaoElixires = 12f;
    [Tooltip("Remove elixires temporarios gerados pela luta depois da derrota do chefe.")]
    [SerializeField] private bool removerElixiresQuandoBossMorrer = true;

    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;
    private Coroutine corrotinaFlash;
    private bool estaMorto = false;
    private readonly List<HealthPickup> elixiresArenaAtivos = new List<HealthPickup>();
    private int quantidadeGeradaArena;
    private Transform kayaNaArena;
    private Coroutine rotinaElixiresArena;



    private void Start()
    {
        vidaAtual = vidaMaxima;
        if (prefabElixirArena != null)
            rotinaElixiresArena = StartCoroutine(GerarElixiresDuranteBatalha());

        todosSprites = GetComponentsInChildren<SpriteRenderer>();
        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }
    }

    // Aceita dano em INT (do seu Projetil.cs e inimigos padrão)
    public void TomarDano(int quantidadeDano)
    {
        TomarDano((float)quantidadeDano);
    }

    // Aceita dano em FLOAT (caso algum ataque use valores decimais)
    public void TomarDano(float quantidadeDano)
    {
        if (estaMorto || quantidadeDano <= 0f) return;
        KayaAudioManager.TocarHitCompartilhado(transform.position);
        vidaAtual -= quantidadeDano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (corrotinaFlash != null) StopCoroutine(corrotinaFlash);
        corrotinaFlash = StartCoroutine(EfeitoFlashDano());

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
    }

    private IEnumerator GerarElixiresDuranteBatalha()
    {
        bool iniciou = false;
        float cronometro = 0f;
        float proximo = Mathf.Max(0f, atrasoPrimeiroElixir);
        while (!estaMorto)
        {
            if (kayaNaArena == null)
            {
                GameObject kaya = GameObject.FindGameObjectWithTag("Player");
                if (kaya != null) kayaNaArena = kaya.transform;
            }

            bool perto = kayaNaArena != null &&
                kayaNaArena.gameObject.activeInHierarchy &&
                Vector2.Distance(transform.position, kayaNaArena.position)
                <= distanciaAtivacaoElixires;

            if (perto && !iniciou) iniciou = true;
            if (perto && iniciou)
            {
                cronometro += Time.deltaTime;
                if (cronometro >= proximo)
                {
                    elixiresArenaAtivos.RemoveAll(x => x == null ||
                        !x.gameObject.activeInHierarchy);

                    bool podeGerar = elixiresArenaAtivos.Count <
                        Mathf.Max(1, maximoElixiresPresentes);
                    bool naoAtingiuTotal = quantidadeTotalElixires == 0 ||
                        quantidadeGeradaArena < quantidadeTotalElixires;

                    if (podeGerar && naoAtingiuTotal && TentarEscolherLocalElixir(out Vector3 posicao))
                    {
                        HealthPickup novo = Instantiate(prefabElixirArena, posicao,
                            Quaternion.identity);
                        novo.ConfigurarComoElixirDeBoss(curaElixirArena);
                        elixiresArenaAtivos.Add(novo);
                        quantidadeGeradaArena++;
                    }

                    cronometro = 0f;
                    proximo = Mathf.Max(0.5f, intervaloSpawnElixires);
                }
            }
            yield return null;
        }
    }

    private bool TentarEscolherLocalElixir(out Vector3 posicao)
    {
        posicao = transform.position;

        if (pontosElixires != null && pontosElixires.Length > 0)
        {
            int inicio = Random.Range(0, pontosElixires.Length);
            for (int i = 0; i < pontosElixires.Length; i++)
            {
                Transform ponto = pontosElixires[(inicio + i) % pontosElixires.Length];
                if (ponto == null) continue;
                posicao = ponto.position;
                return true;
            }
        }

        // Se nao houver pontos, tenta criar sobre o chao real, e nunca
        // no ar ou atravessando uma parede.
        for (int tentativa = 0; tentativa < 12; tentativa++)
        {
            float x = transform.position.x +
                Random.Range(-raioSpawnElixires, raioSpawnElixires);
            Vector2 origem = new Vector2(x, transform.position.y + 300f);
            RaycastHit2D[] impactos = Physics2D.RaycastAll(
                origem, Vector2.down, 900f, chaoParaElixires);
            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null || impacto.collider.isTrigger ||
                    impacto.normal.y < 0.55f) continue;
                posicao = new Vector3(impacto.point.x,
                    impacto.point.y + alturaAcimaDoChaoElixires,
                    transform.position.z);
                return true;
            }
        }

        return false;
    }

    private void EncerrarElixiresDaArena()
    {
        if (rotinaElixiresArena != null)
        {
            StopCoroutine(rotinaElixiresArena);
            rotinaElixiresArena = null;
        }

        if (!removerElixiresQuandoBossMorrer) return;
        foreach (HealthPickup elixir in elixiresArenaAtivos)
            if (elixir != null) Destroy(elixir.gameObject);
        elixiresArenaAtivos.Clear();
    }

    private void OnDisable()
    {
        EncerrarElixiresDaArena();
    }

    private IEnumerator EfeitoFlashDano()
    {
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
            {
                todosSprites[i].color = corDano;
            }
        }

        yield return new WaitForSeconds(duracaoFlash);

        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
            {
                todosSprites[i].color = coresOriginais[i];
            }
        }
    }

    private void Morrer()
    {
        estaMorto = true;
        EncerrarElixiresDaArena();

        BossGuerraController controller = GetComponent<BossGuerraController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Morte");
        }

        Destroy(gameObject, 2f);
    }

    public float GetVidaAtual() => vidaAtual;
    public float GetVidaMaxima() => vidaMaxima;
}