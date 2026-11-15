using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDJuice : MonoBehaviour
{
    [Header("Tremor do HUD")]
    [SerializeField] private RectTransform painelHUD;
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
            float x = Random.Range(-1f, 1f) * intensidadeTremor;
            float y = Random.Range(-1f, 1f) * intensidadeTremor;

            painelHUD.anchoredPosition = posicaoOriginalHUD + new Vector3(x, y, 0f);

            tempoPassado += Time.deltaTime;
            yield return null;
        }

        painelHUD.anchoredPosition = posicaoOriginalHUD;
    }

    public void AtualizarRetrato(float porcentagemVida)
    {
        if (imagemRetrato == null) return;

        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
        {
            if (fotoFuria != null) imagemRetrato.sprite = fotoFuria;
        }
        else if (porcentagemVida <= 0.2f)
        {
            if (fotoVidaBaixa != null) imagemRetrato.sprite = fotoVidaBaixa;
        }
        else
        {
            if (fotoNormal != null) imagemRetrato.sprite = fotoNormal;
        }
    }
}