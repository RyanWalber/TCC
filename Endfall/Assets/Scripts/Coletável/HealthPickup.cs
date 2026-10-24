using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class HealthPickup : MonoBehaviour
{
    [Header("Configuracao do elixir")]
    [Min(1f)] [SerializeField] private float quantidadeVida = 25f;

    [Tooltip("Identificador persistente e exclusivo desta instancia na cena.")]
    [SerializeField] private string idUnico;

    [Header("Flutuacao do visual (nao move o objeto principal)")]
    [SerializeField] private Transform visualFlutuante;
    [Min(0f)] [SerializeField] private float alturaFlutuacao = 0.25f;
    [Min(0f)] [SerializeField] private float velocidadeFlutuacao = 2f;

    private Vector3 posicaoVisualInicial;
    private float faseFlutuacao;

    private void Awake()
    {
        // O pai fica parado: assim cada elixir conserva sua posicao no mapa.
        // Apenas o visual filho se movimenta para cima e para baixo.
        if (visualFlutuante == null && transform.childCount > 0)
            visualFlutuante = transform.GetChild(0);

        if (visualFlutuante != null)
            posicaoVisualInicial = visualFlutuante.localPosition;

        faseFlutuacao = transform.position.x * 0.31f + transform.position.y * 0.17f;
    }

    private void Update()
    {
        if (visualFlutuante != null && alturaFlutuacao > 0f && velocidadeFlutuacao > 0f)
        {
            float deslocamento = Mathf.Sin(Time.time * velocidadeFlutuacao + faseFlutuacao) * alturaFlutuacao;
            visualFlutuante.localPosition = posicaoVisualInicial + Vector3.up * deslocamento;
        }
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(idUnico))
        {
            // Um ID aleatorio em Start mudaria a cada partida e quebraria o save.
            Debug.LogError("[HealthPickup] Elixir sem ID persistente. Salve a cena no Editor para gerar IDs unicos.", this);
            return;
        }

        if (SaveManager.Instance != null && SaveManager.Instance.ElixirVidaJaFoiColetado(idUnico))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth vida = other.GetComponent<PlayerHealth>();
        if (vida == null) vida = other.GetComponentInParent<PlayerHealth>();
        if (vida == null || vida.VidaAtual >= vida.VidaMaxima) return;

        vida.RecuperarVida(quantidadeVida);

        if (SaveManager.Instance != null && !string.IsNullOrWhiteSpace(idUnico))
            SaveManager.Instance.RegistrarElixirVidaColetado(idUnico);

        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    // Valida apos o Unity terminar duplicacoes e carregamentos de cena.
    // OnValidate pode ocorrer durante desserializacao: nao modifique a cena nele.
    private void OnValidate()
    {
        AgendarValidacao();
    }

    [InitializeOnLoadMethod]
    private static void RegistrarValidacaoAutomatica()
    {
        EditorApplication.hierarchyChanged -= AgendarValidacao;
        EditorApplication.hierarchyChanged += AgendarValidacao;
        AgendarValidacao();
    }

    private static void AgendarValidacao()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorApplication.delayCall -= GarantirIdsUnicosNasCenasAbertas;
        EditorApplication.delayCall += GarantirIdsUnicosNasCenasAbertas;
    }

    private static void GarantirIdsUnicosNasCenasAbertas()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var encontrados = FindObjectsByType<HealthPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        var elixiresDaCena = new List<HealthPickup>();
        foreach (HealthPickup elixir in encontrados)
        {
            if (elixir == null || !elixir.gameObject.scene.IsValid() ||
                !elixir.gameObject.scene.isLoaded || PrefabUtility.IsPartOfPrefabAsset(elixir))
                continue;

            elixiresDaCena.Add(elixir);
        }

        // Mantem um ID antigo por grupo: salva o maior numero possivel
        // de compatibilidades com os saves ja existentes.
        elixiresDaCena.Sort((a, b) =>
            string.CompareOrdinal(
                GlobalObjectId.GetGlobalObjectIdSlow(a).ToString(),
                GlobalObjectId.GetGlobalObjectIdSlow(b).ToString()));

        var utilizados = new HashSet<string>(StringComparer.Ordinal);

        foreach (HealthPickup elixir in elixiresDaCena)
        {
            // Um prefab duplicado pode herdar o mesmo ID em cenas DIFERENTES.
            // Nao basta procurar duplicatas so na cena atualmente aberta:
            // IDs herdados do asset precisam virar overrides exclusivos.
            HealthPickup origem = PrefabUtility.GetCorrespondingObjectFromSource(elixir);
            bool idHerdadoDoPrefab = origem != null &&
                !string.IsNullOrWhiteSpace(elixir.idUnico) &&
                elixir.idUnico == origem.idUnico;

            if (!idHerdadoDoPrefab &&
                !string.IsNullOrWhiteSpace(elixir.idUnico) &&
                utilizados.Add(elixir.idUnico))
                continue;

            string novoId;
            do { novoId = Guid.NewGuid().ToString("N"); }
            while (!utilizados.Add(novoId));

            Undo.RecordObject(elixir, "Corrigir ID do elixir de vida");
            elixir.idUnico = novoId;
            EditorUtility.SetDirty(elixir);
            PrefabUtility.RecordPrefabInstancePropertyModifications(elixir);
            EditorSceneManager.MarkSceneDirty(elixir.gameObject.scene);
        }
    }
#endif
}
