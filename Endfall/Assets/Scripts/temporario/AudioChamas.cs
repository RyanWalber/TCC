using UnityEngine;

// Fica no objeto das particulas de fogo, com volume crescente ao acender.
[DisallowMultipleComponent]
public class AudioChamas : MonoBehaviour
{
    [Header("Som das chamas (neste objeto)")]
    [SerializeField] private AudioClip somChamas;
    [SerializeField, Range(0f, 1f)] private float volumeMaximo = 0.65f;
    [SerializeField, Min(0.1f)] private float tempoParaCrescer = 1.6f;
    [SerializeField, Min(0.1f)] private float tempoParaApagar = 0.5f;
    [SerializeField] private ParticleSystem fogo;

    private AudioSource fonte;
    private bool aceso;
    private float progresso;

    public void Configurar(ParticleSystem particulas, AudioClip padrao)
    {
        if (particulas != null) fogo = particulas;
        if (somChamas == null) somChamas = padrao;
    }

    private void Awake()
    {
        if (fogo == null) fogo = GetComponent<ParticleSystem>();
        fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.volume = 0f;
    }

    public void Acender()
    {
        aceso = true;
        progresso = 0f;
        if (somChamas == null || fonte == null) return;
        fonte.Stop();
        fonte.clip = somChamas;
        fonte.volume = 0f;
        fonte.Play();
    }

    private void Update()
    {
        if (fonte == null) return;
        bool ativo = aceso && (fogo == null || fogo.IsAlive(true));
        float duracao = Mathf.Max(0.1f, ativo ? tempoParaCrescer : tempoParaApagar);
        progresso = Mathf.MoveTowards(progresso, ativo ? 1f : 0f,
            Time.deltaTime / duracao);
        fonte.volume = volumeMaximo * Mathf.SmoothStep(0f, 1f, progresso);
        if (!ativo && progresso <= 0f && fonte.isPlaying)
        {
            fonte.Stop();
            aceso = false;
        }
    }
}
