using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Realce amarelo ao passar o mouse sobre botoes UI.
// Automatico: muda o fundo quando visivel; se ele for transparente,
// muda o texto (Text ou TextMeshPro). Tambem suporta botoes-imagem.
// Nao altera os metodos OnClick nem os sons de clique.
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class EfeitoHoverBotao : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TipoRealce { Automatico, Texto, Imagem, TextoEImagem }

    [Header("Destaque ao passar o mouse")]
    [SerializeField] private TipoRealce tipoRealce = TipoRealce.Automatico;
    [SerializeField] private Color amarelo = new Color(1f, 0.8f, 0.22f, 1f);
    [Min(0f)] [SerializeField] private float tempoDeTransicao = 0.12f;

    private Button botao;
    private bool registrado;
    private bool destacarPeloButton;
    private readonly List<Graphic> visuais = new List<Graphic>();
    private readonly List<Color> coresOriginais = new List<Color>();
    private Coroutine transicaoEmAndamento;

    private void Awake()
    {
        Registrar();
    }

    public void Registrar()
    {
        if (registrado) return;
        botao = GetComponent<Button>();
        if (botao == null) return;
        registrado = true;

        Graphic alvoDoBotao = botao.targetGraphic;
        bool fundoVisivel = alvoDoBotao != null &&
            !(alvoDoBotao is Text) && !(alvoDoBotao is TMP_Text) &&
            alvoDoBotao.color.a > 0.05f && alvoDoBotao.gameObject.activeSelf;

        bool escolherImagem = tipoRealce == TipoRealce.Imagem ||
            tipoRealce == TipoRealce.TextoEImagem ||
            (tipoRealce == TipoRealce.Automatico && fundoVisivel);
        bool escolherTexto = tipoRealce == TipoRealce.Texto ||
            tipoRealce == TipoRealce.TextoEImagem ||
            (tipoRealce == TipoRealce.Automatico && !fundoVisivel);

        // A Unity ja controla transicoes de cor para o Graphic alvo do
        // Button. Usar Highlighted Color evita conflitos com Color Tint.
        if (botao.transition == Selectable.Transition.ColorTint &&
            alvoDoBotao != null && alvoDoBotao.color.a > 0.05f &&
            ((escolherImagem && !(alvoDoBotao is Text) && !(alvoDoBotao is TMP_Text)) ||
             (escolherTexto && (alvoDoBotao is Text || alvoDoBotao is TMP_Text))))
        {
            ColorBlock cores = botao.colors;
            cores.highlightedColor = amarelo;
            cores.fadeDuration = tempoDeTransicao;
            botao.colors = cores;
            destacarPeloButton = true;
        }

        Graphic[] filhos = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in filhos)
        {
            if (graphic == null || graphic.color.a < 0.05f)
                continue;

            // Nao recolorir elementos que pertencem a outro botao filho.
            Button dono = graphic.GetComponentInParent<Button>(true);
            if (dono != botao) continue;

            bool eTexto = graphic is TMP_Text || graphic is Text;
            bool incluir = eTexto ? escolherTexto : escolherImagem;
            if (!incluir) continue;

            // Evita dois sistemas disputando a cor do mesmo objeto.
            if (graphic == alvoDoBotao && destacarPeloButton) continue;

            // No automatico, se o fundo visivel ja for destacado, nao
            // muda tambem a cor das letras (mantem contraste dos rotulos).
            visuais.Add(graphic);
            coresOriginais.Add(graphic.color);
        }

        // Alguns botoes de imagem possuem alvo invisivel, com Sprite
        // visivel no filho; esses tambem sao tratados pela lista acima.
        if (tipoRealce == TipoRealce.Automatico && visuais.Count == 0 &&
            !destacarPeloButton)
        {
            foreach (Graphic graphic in filhos)
            {
                if (graphic != null && graphic.color.a > 0.05f &&
                    graphic.GetComponentInParent<Button>(true) == botao)
                {
                    visuais.Add(graphic);
                    coresOriginais.Add(graphic.color);
                    break;
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Registrar();
        if (botao == null || !botao.IsActive() || !botao.IsInteractable()) return;
        Animar(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Animar(false);
    }

    private void Animar(bool destacar)
    {
        if (transicaoEmAndamento != null)
        {
            StopCoroutine(transicaoEmAndamento);
            transicaoEmAndamento = null;
        }

        if (visuais.Count == 0) return;

        if (!isActiveAndEnabled || tempoDeTransicao <= 0f)
        {
            AplicarCor(destacar);
            return;
        }

        transicaoEmAndamento = StartCoroutine(Transicionar(destacar));
    }

    private IEnumerator Transicionar(bool destacar)
    {
        Color[] inicio = new Color[visuais.Count];
        for (int i = 0; i < visuais.Count; i++)
            if (visuais[i] != null) inicio[i] = visuais[i].color;

        float decorrido = 0f;
        while (decorrido < tempoDeTransicao)
        {
            decorrido += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(decorrido / tempoDeTransicao);
            t = t * t * (3f - 2f * t);
            for (int i = 0; i < visuais.Count; i++)
            {
                if (visuais[i] == null) continue;
                Color destino = CorAlvo(i, destacar);
                visuais[i].color = Color.Lerp(inicio[i], destino, t);
            }
            yield return null;
        }

        AplicarCor(destacar);
        transicaoEmAndamento = null;
    }

    private Color CorAlvo(int indice, bool destacar)
    {
        Color original = coresOriginais[indice];
        // Preserva transparencia da arte original.
        return destacar ? new Color(amarelo.r, amarelo.g, amarelo.b, original.a) : original;
    }

    private void AplicarCor(bool destacar)
    {
        for (int i = 0; i < visuais.Count; i++)
            if (visuais[i] != null) visuais[i].color = CorAlvo(i, destacar);
    }

    private void OnDisable()
    {
        if (transicaoEmAndamento != null)
        {
            StopCoroutine(transicaoEmAndamento);
            transicaoEmAndamento = null;
        }
        AplicarCor(false);
    }

    private void OnDestroy()
    {
        if (transicaoEmAndamento != null)
            StopCoroutine(transicaoEmAndamento);
    }
}
