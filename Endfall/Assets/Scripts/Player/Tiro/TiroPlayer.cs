using UnityEngine;

public class TiroPlayer : MonoBehaviour
{
    [Header("Configurações do Tiro")]
    public GameObject prefabProjetil;
    public GameObject prefabEfeitoFogo;
    public Transform pontoDeDisparo;
    public KeyCode teclaTiro = KeyCode.J;
    public float cadenciaTiro = 0.2f;

    [Header("Áudio do tiro")]
    [SerializeField] private AudioSource audioTiro;
    [SerializeField, Range(0f, 1f)] private float volumeTiro = 0.7f;

    [Header("Respiração da Kaya")]
    [SerializeField] private AudioSource audioRespiracao;
    [SerializeField, Range(0f, 1f)] private float volumeRespiracaoNormal = 0.12f;
    [SerializeField, Range(0f, 1f)] private float volumeRespiracaoCansada = 0.65f;
    [SerializeField, Min(0f)] private float esperaDepoisCombate = 3f;
    [SerializeField, Min(0.1f)] private float tempoRecuperacao = 8f;

    public AudioSource FonteAudioTiro => audioTiro;
    public AudioSource FonteAudioRespiracao => audioRespiracao;
    private KayaAudioManager audioKaya;

    private float tempoProximoTiro;
    private float ultimoEventoCombate = -1000f;
    private bool recuperandoFolego;
    private float inicioRecuperacao;
    private SistemaFuria sistemaFuria;

    private void Start()
    {
        sistemaFuria = GetComponentInParent<SistemaFuria>();
        audioKaya = GetComponentInParent<KayaAudioManager>();

        if (audioKaya == null && audioRespiracao != null)
        {
            audioRespiracao.loop = true;
            audioRespiracao.volume = volumeRespiracaoNormal;
            if (audioRespiracao.clip != null && !audioRespiracao.isPlaying)
                audioRespiracao.Play();
        }
    }

    private void Update()
    {
        float cadenciaEfetiva = cadenciaTiro;
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
            cadenciaEfetiva *= sistemaFuria.multCadencia;

        if ((Input.GetKeyDown(teclaTiro) || Input.GetMouseButtonDown(0)) && Time.time >= tempoProximoTiro)
        {
            if (Atirar())
                tempoProximoTiro = Time.time + cadenciaEfetiva;
        }

        if (audioKaya == null) AtualizarRespiracao();
    }

    private bool Atirar()
    {
        if (prefabProjetil == null || pontoDeDisparo == null) return false;

        GameObject tiro = Instantiate(prefabProjetil, pontoDeDisparo.position, pontoDeDisparo.rotation);
        Projetil scriptProjetil = tiro.GetComponent<Projetil>();
        if (scriptProjetil != null)
            scriptProjetil.Disparar();

        if (prefabEfeitoFogo != null)
            Instantiate(prefabEfeitoFogo, pontoDeDisparo.position, pontoDeDisparo.rotation, pontoDeDisparo);

        // PlayOneShot permite disparos seguidos sem cortar o som anterior.
        if (audioKaya != null) audioKaya.TocarTiro();
        else if (audioTiro != null && audioTiro.clip != null)
            audioTiro.PlayOneShot(audioTiro.clip, volumeTiro);

        RegistrarAcaoCombate();
        return true;
    }

    public void RegistrarAcaoCombate()
    {
        if (audioKaya != null) { audioKaya.RegistrarCombate(); return; }
        ultimoEventoCombate = Time.time;
        recuperandoFolego = false;
        if (audioRespiracao != null)
            audioRespiracao.volume = volumeRespiracaoNormal;
    }

    private void AtualizarRespiracao()
    {
        if (audioRespiracao == null || audioRespiracao.clip == null) return;

        if (!audioRespiracao.isPlaying)
            audioRespiracao.Play();

        if (!recuperandoFolego && Time.time >= ultimoEventoCombate + esperaDepoisCombate
            && ultimoEventoCombate > -100f)
        {
            recuperandoFolego = true;
            inicioRecuperacao = Time.time;
            ultimoEventoCombate = -1000f;
        }

        if (!recuperandoFolego) return;

        float progresso = Mathf.Clamp01((Time.time - inicioRecuperacao) / tempoRecuperacao);
        audioRespiracao.volume = Mathf.Lerp(volumeRespiracaoCansada, volumeRespiracaoNormal, progresso);
        if (progresso >= 1f)
            recuperandoFolego = false;
    }
}
