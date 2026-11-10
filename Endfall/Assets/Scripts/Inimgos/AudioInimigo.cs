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
    [Tooltip("Intervalo entre vozes espontaneas, se Som Proprio estiver preenchido. Zero desliga a repeticao.")]
    [SerializeField, Min(0f)] private float intervaloSomProprio = 8f;

    [NonSerialized] private Transform kaya;
    [NonSerialized] private AudioSource fonte;
    [NonSerialized] private float proximoSomProprio;
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

    public void Atualizar(Component emissor)
    {
        if (morreu || somProprio == null || intervaloSomProprio <= 0f ||
            Time.time < proximoSomProprio) return;
        TocarSomProprio(emissor);
    }

    public void TocarSomProprio(Component emissor)
    {
        if (Time.time < proximoSomProprio) return;
        if (Tocar(emissor, somProprio, volumeProprio))
            proximoSomProprio = Time.time + Mathf.Max(0.1f, intervaloSomProprio);
    }

    public void TocarAtaque(Component emissor) =>
        Tocar(emissor, somAtaque, volumeAtaque);

    public void TocarHitCausado(Component emissor)
    {
        if (Time.unscaledTime - ultimoHitCausado < intervaloHits) return;
        if (Tocar(emissor, hitCausado, volumeHitCausado))
            ultimoHitCausado = Time.unscaledTime;
    }

    public void TocarHitRecebido(Component emissor)
    {
        if (Time.unscaledTime - ultimoHitRecebido < intervaloHits) return;
        if (Tocar(emissor, hitRecebido, volumeHitRecebido))
            ultimoHitRecebido = Time.unscaledTime;
    }

    // Sons exclusivos: cuspida da Peste, corvos e foice da Morte, etc.
    public void TocarEspecial(Component emissor, AudioClip clip, float volume, Vector3? origem = null) =>
        Tocar(emissor, clip, volume, origem);

    public void TocarMorte(Component emissor)
    {
        if (morreu) return;
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
