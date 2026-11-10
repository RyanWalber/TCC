using UnityEngine;

// Adicione este componente ao prefab de CADA inimigo para configurar sons independentes.
[DisallowMultipleComponent]
[AddComponentMenu("EndFall/Audio/Audio Individual do Inimigo")]
public class AudioInimigo : MonoBehaviour
{
    [Header("Fonte de audio (opcional)")]
    [SerializeField] private AudioSource fonte;

    [Header("HIT - separados por personagem")]
    [SerializeField] private AudioClip hitAoDarDano;
    [SerializeField] private AudioClip hitAoReceberDano;
    [SerializeField] private AudioClip somMorte;

    [Header("Ataque e som caracteristico")]
    [SerializeField] private AudioClip somAtaque;
    [SerializeField] private AudioClip somCaracteristico;

    [Header("Especiais (preencha somente os usados por este inimigo)")]
    [SerializeField] private AudioClip investidaOuDash;
    [SerializeField] private AudioClip cuspidaPeste;
    [SerializeField] private AudioClip lancamentoCorvos;
    [SerializeField] private AudioClip golpeFoice;

    [Header("Distancia e volume")]
    [SerializeField, Min(0f)] private float distanciaMaxima = 150f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField, Min(0f)] private float intervaloMinimoHit = 0.08f;
    [SerializeField, Min(0f)] private float intervaloSomCaracteristico = 7f;
    [SerializeField, Min(0f)] private float variacaoIntervalo = 2f;

    private Transform kaya;
    private float proximoSomCaracteristico;
    private float ultimoHitCausado = float.NegativeInfinity;
    private float ultimoHitRecebido = float.NegativeInfinity;
    private bool morreu;

    private void Awake()
    {
        if (fonte == null) fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
        AgendarSom();
    }

    private void Update()
    {
        if (morreu || somCaracteristico == null || Time.time < proximoSomCaracteristico) return;
        Tocar(somCaracteristico);
        AgendarSom();
    }

    private void AgendarSom()
    {
        float variacao = Mathf.Max(0f, variacaoIntervalo);
        proximoSomCaracteristico = Time.time +
            Mathf.Max(0.2f, intervaloSomCaracteristico + Random.Range(-variacao, variacao));
    }

    private bool PertoDaKaya()
    {
        if (kaya == null)
        {
            GameObject jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador != null) kaya = jogador.transform;
        }
        return kaya != null &&
            Vector2.Distance(transform.position, kaya.position) <= distanciaMaxima;
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && !morreu && fonte != null && PertoDaKaya())
            fonte.PlayOneShot(clip, volume);
    }

    public void TocarHitCausado()
    {
        if (Time.time - ultimoHitCausado < intervaloMinimoHit) return;
        ultimoHitCausado = Time.time;
        Tocar(hitAoDarDano);
    }

    public void TocarHitRecebido()
    {
        if (Time.time - ultimoHitRecebido < intervaloMinimoHit) return;
        ultimoHitRecebido = Time.time;
        Tocar(hitAoReceberDano);
    }

    public void TocarAtaque() => Tocar(somAtaque);
    public void TocarInvestida() => Tocar(investidaOuDash);
    public void TocarCuspida() => Tocar(cuspidaPeste);
    public void TocarCorvos() => Tocar(lancamentoCorvos);
    public void TocarFoice() => Tocar(golpeFoice);

    // O objeto morre na mesma frame em varios inimigos; uma fonte temporaria
    // permite ouvir o clip inteiro sem alterar a logica de checkpoint.
    public void TocarMorte()
    {
        if (morreu) return;
        bool audivel = PertoDaKaya();
        morreu = true;
        if (!audivel || somMorte == null) return;

        GameObject temporario = new GameObject("Audio - Morte Inimigo");
        temporario.transform.position = transform.position;
        AudioSource saida = temporario.AddComponent<AudioSource>();
        saida.playOnAwake = false;
        saida.spatialBlend = 0f;
        saida.PlayOneShot(somMorte, volume);
        Destroy(temporario, somMorte.length + 0.1f);
    }
}
