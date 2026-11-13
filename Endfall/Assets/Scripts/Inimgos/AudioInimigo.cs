using System;
using UnityEngine;

// Configuracao serializavel: aparece no Inspector DO PROPRIO inimigo/boss.
// Nao precisa colocar outro componente em prefabs nem referenciar a Kaya.
[Serializable]
public sealed class AudioInimigo
{
    [Header("Hit - cada inimigo tem os seus clips")]
    [SerializeField] private AudioClip hitCausado;
    [SerializeField] private AudioClip hitRecebido;
    [SerializeField] private AudioClip morte;

    [Header("Voz / acao do inimigo")]
    [SerializeField] private AudioClip somProprio;
    [SerializeField] private AudioClip somAtaque;

    [Header("Ao aparecer / ser invocado (som deste personagem)")]
    [SerializeField] private AudioClip somInvocacao;
    [SerializeField, Range(0f, 1f)] private float volumeInvocacao = 0.65f;
    [SerializeField, Range(0f, 0.25f)] private float variacaoPitchInvocacao = 0.08f;

    [Header("Volume de cada tipo")]
    [SerializeField, Range(0f, 1f)] private float volumeHitCausado = 0.75f;
    [SerializeField, Range(0f, 1f)] private float volumeHitRecebido = 0.75f;
    [SerializeField, Range(0f, 1f)] private float volumeMorte = 0.8f;
    [SerializeField, Range(0f, 1f)] private float volumeProprio = 0.7f;
    [SerializeField, Range(0f, 1f)] private float volumeAtaque = 0.7f;

    [Header("Distancia e repeticao")]
    [Tooltip("Alcance dos sons deste inimigo, em unidades do mundo.")]
    [SerializeField, Min(1f)] private float alcance = 350f;
    [Tooltip("Intervalo minimo entre hits do mesmo tipo para evitar spam.")]
    [SerializeField, Min(0f)] private float intervaloHits = 0.055f;
    [Tooltip("Tempo do fade do loop do inimigo ao entrar ou sair de alcance.")]
    [SerializeField, Min(0.05f)] private float tempoFadeSomProprio = 0.4f;

    [NonSerialized] private Transform kaya;
    [NonSerialized] private AudioSource fonte;
    [NonSerialized] private AudioSource fonteSomProprio;
    [NonSerialized] private float ultimoHitCausado = float.NegativeInfinity;
    [NonSerialized] private float ultimoHitRecebido = float.NegativeInfinity;
    [NonSerialized] private bool morreu;

    private bool EmAlcance(Vector3 origem)
    {
        if (kaya == null)
        {
            GameObject jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador != null) kaya = jogador.transform;
        }

        return kaya != null &&
            Vector2.Distance(origem, kaya.position) <= Mathf.Max(1f, alcance);
    }

    private AudioSource ObterFonte(Component emissor)
    {
        if (fonte != null) return fonte;
        GameObject filho = new GameObject("Audio - " + emissor.gameObject.name);
        filho.transform.SetParent(emissor.transform, false);
        fonte = filho.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f; // a distancia e calculada manualmente em 2D
        return fonte;
    }

    private bool Tocar(Component emissor, AudioClip clip, float volume, Vector3? origem = null)
    {
        if (emissor == null || clip == null || morreu) return false;
        Vector3 ponto = origem ?? emissor.transform.position;
        if (!EmAlcance(ponto)) return false;

        float distancia = Vector2.Distance(ponto, kaya.position);
        float fator = Mathf.Lerp(1f, 0.2f,
            Mathf.Clamp01(distancia / Mathf.Max(1f, alcance)));
        ObterFonte(emissor).PlayOneShot(clip, Mathf.Clamp01(volume) * fator);
        return true;
    }

    // Uma fonte TEMPORARIA por invocacao. O som pertence a este personagem,
    // continua mesmo que o projetil seja destruido e permite varios corvos simultaneos.
    private bool TocarIndependente(Component emissor, AudioClip clip, float volume,
        bool variarPitch = false, bool ignorarAlcance = false)
    {
        if (emissor == null || clip == null || morreu) return false;
        Vector3 origem = emissor.transform.position;
        if (!ignorarAlcance && !EmAlcance(origem)) return false;

        // Sons de invocados (ex.: corvos) sao 2D, audiveis por instancia
        // e nao devem perder volume por estarem distantes da Kaya.
        float fator = 1f;
        if (!ignorarAlcance)
        {
            float distancia = Vector2.Distance(origem, kaya.position);
            fator = Mathf.Lerp(1f, 0.2f,
                Mathf.Clamp01(distancia / Mathf.Max(1f, alcance)));
        }

        GameObject temporario = new GameObject("Audio - " + emissor.gameObject.name);
        temporario.transform.position = origem;
        AudioSource saida = temporario.AddComponent<AudioSource>();
        saida.playOnAwake = false;
        saida.spatialBlend = 0f;
        saida.pitch = variarPitch
            ? UnityEngine.Random.Range(1f - variacaoPitchInvocacao,
                                       1f + variacaoPitchInvocacao)
            : 1f;
        saida.PlayOneShot(clip, Mathf.Clamp01(volume) * fator);
        UnityEngine.Object.Destroy(temporario, clip.length / Mathf.Max(0.1f, saida.pitch) + 0.2f);
        return true;
    }

    public bool TocarInvocacao(Component emissor, bool ignorarAlcance = false) =>
        TocarIndependente(emissor, somInvocacao, volumeInvocacao, true, ignorarAlcance);

    // O som proprio e uma camada em loop INDEPENDENTE dos hits e ataques.
    public void Atualizar(Component emissor)
    {
        if (morreu || emissor == null || somProprio == null)
        {
            PararSomProprio();
            return;
        }

        bool perto = EmAlcance(emissor.transform.position);
        if (perto && fonteSomProprio == null)
        {
            GameObject filho = new GameObject("Audio - Som Proprio (Loop)");
            filho.transform.SetParent(emissor.transform, false);
            fonteSomProprio = filho.AddComponent<AudioSource>();
            fonteSomProprio.playOnAwake = false;
            fonteSomProprio.loop = true;
            fonteSomProprio.spatialBlend = 0f;
            fonteSomProprio.volume = 0f;
            fonteSomProprio.clip = somProprio;
        }

        if (fonteSomProprio == null) return;
        if (fonteSomProprio.clip != somProprio)
        {
            fonteSomProprio.Stop();
            fonteSomProprio.clip = somProprio;
        }
        if (perto && !fonteSomProprio.isPlaying)
            fonteSomProprio.Play();

        float atenuacao = 0f;
        if (perto && kaya != null)
        {
            float distancia = Vector2.Distance(emissor.transform.position, kaya.position);
            atenuacao = Mathf.Lerp(1f, 0.2f,
                Mathf.Clamp01(distancia / Mathf.Max(1f, alcance)));
        }

        float destino = perto ? Mathf.Clamp01(volumeProprio) * atenuacao : 0f;
        float passo = Time.deltaTime * Mathf.Max(0.05f, volumeProprio) /
            Mathf.Max(0.05f, tempoFadeSomProprio);
        fonteSomProprio.volume = Mathf.MoveTowards(
            fonteSomProprio.volume, destino, passo);

        if (!perto && fonteSomProprio.isPlaying && fonteSomProprio.volume <= 0.001f)
            fonteSomProprio.Stop();
    }

    public void TocarSomProprio(Component emissor) => Atualizar(emissor);

    private void PararSomProprio()
    {
        if (fonteSomProprio == null) return;
        fonteSomProprio.Stop();
        fonteSomProprio.volume = 0f;
    }

    public void TocarAtaque(Component emissor) =>
        Tocar(emissor, somAtaque, volumeAtaque);

    public void TocarHitCausado(Component emissor, bool continuarAposDestruir = false)
    {
        if (Time.unscaledTime - ultimoHitCausado < intervaloHits) return;
        bool tocou = continuarAposDestruir
            ? TocarIndependente(emissor, hitCausado, volumeHitCausado)
            : Tocar(emissor, hitCausado, volumeHitCausado);
        if (tocou) ultimoHitCausado = Time.unscaledTime;
    }

    public void TocarHitRecebido(Component emissor, bool continuarAposDestruir = false)
    {
        if (Time.unscaledTime - ultimoHitRecebido < intervaloHits) return;
        bool tocou = continuarAposDestruir
            ? TocarIndependente(emissor, hitRecebido, volumeHitRecebido)
            : Tocar(emissor, hitRecebido, volumeHitRecebido);
        if (tocou) ultimoHitRecebido = Time.unscaledTime;
    }

    // Sons exclusivos: cuspida da Peste, corvos e foice da Morte, etc.
    public void TocarEspecial(Component emissor, AudioClip clip, float volume, Vector3? origem = null) =>
        Tocar(emissor, clip, volume, origem);

    public void TocarMorte(Component emissor)
    {
        if (morreu) return;
        PararSomProprio();
        bool audivel = emissor != null && morte != null &&
            EmAlcance(emissor.transform.position);
        morreu = true;
        if (!audivel) return;

        // O inimigo pode ser destruido imediatamente; o clip continua tocando.
        GameObject temporario = new GameObject("Audio - Morte " + emissor.gameObject.name);
        AudioSource saida = temporario.AddComponent<AudioSource>();
        saida.playOnAwake = false;
        saida.spatialBlend = 0f;
        float distancia = Vector2.Distance(emissor.transform.position, kaya.position);
        float fator = Mathf.Lerp(1f, 0.2f,
            Mathf.Clamp01(distancia / Mathf.Max(1f, alcance)));
        saida.PlayOneShot(morte, Mathf.Clamp01(volumeMorte) * fator);
        UnityEngine.Object.Destroy(temporario, morte.length + 0.2f);
    }

    // BossVida e VidaBossGuerra localizam os clips no controller do proprio boss.
    public static void NotificarVidaBoss(Component componenteVida, bool morreu)
    {
        if (componenteVida == null) return;
        MonoBehaviour[] componentes = componenteVida.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour componente in componentes)
        {
            IAudioBoss dono = componente as IAudioBoss;
            if (dono == null) continue;
            if (morreu) dono.AudioAoMorrer();
            else dono.AudioAoReceberHit();
            return;
        }
    }
}

public interface IAudioBoss
{
    void AudioAoReceberHit();
    void AudioAoMorrer();
}
