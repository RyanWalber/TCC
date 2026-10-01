using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDJuice : MonoBehaviour
{
    [Header("Tremor do HUD")]
    [SerializeField] private RectTransform painelHUD; // Arraste o objeto pai de todo o HUD aqui
    [SerializeField] private float duracaoTremor = 0.15f;
    [SerializeField] private float intensidadeTremor = 10f;
    private Vector3 posicaoOriginalHUD;

    [Header("Retrato da Kaya")]
    [SerializeField] private Image imagemRetrato;
    [SerializeField] private Sprite fotoNormal;
    [SerializeField] private Sprite fotoFuria;
    [SerializeField] private Sprite fotoVidaBaixa;

    private SistemaFuria sistemaFuria;

    private void Start()
    {
        sistemaFuria = FindFirstObjectByType<SistemaFuria>();

        if (painelHUD != null)
        {
            posicaoOriginalHUD = painelHUD.anchoredPosition;
        }
    }

    // Chama o tremor quando leva dano
    public void TremerHUD()
    {
        if (painelHUD != null)
        {
            StopAllCoroutines();
            StartCoroutine(RoutineShake());
        }
    }

    private IEnumerator RoutineShake()
    {
        float tempoPassado = 0f;

        while (tempoPassado < duracaoTremor)
        {
            // Gera um pequeno deslocamento aleatório
            float x = Random.Range(-1f, 1f) * intensidadeTremor;
            float y = Random.Range(-1f, 1f) * intensidadeTremor;

            painelHUD.anchoredPosition = posicaoOriginalHUD + new Vector3(x, y, 0f);

            tempoPassado += Time.deltaTime;
            yield return null;
        }

        // Volta para a posição normal
        painelHUD.anchoredPosition = posicaoOriginalHUD;
    }

    // Troca a foto do rosto da Kaya
    public void AtualizarRetrato(float porcentagemVida)
    {
        if (imagemRetrato == null) return;

        // Prioridade 1: Se estiver em Fúria
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
        {
            if (fotoFuria != null) imagemRetrato.sprite = fotoFuria;
        }
        // Prioridade 2: Vida crítica (menor que 20%)
        else if (porcentagemVida <= 0.2f)
        {
            if (fotoVidaBaixa != null) imagemRetrato.sprite = fotoVidaBaixa;
        }
        // Normal
        else
        {
            if (fotoNormal != null) imagemRetrato.sprite = fotoNormal;
        }
    }
}