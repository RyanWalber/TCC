using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossPesteInvocacao : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab original ou variante do InimigoParado, nunca o objeto da cena.")]
    [SerializeField] private GameObject prefabInimigoParado;
    [SerializeField] private Transform kaya;

    [Header("Encontrar chao abaixo da Kaya")]
    [Tooltip("Layers dos pisos e plataformas em que o inimigo pode emergir.")]
    [SerializeField] private LayerMask camadasChao = 1 << 3;
    [Min(0f)] [SerializeField] private float inicioRaioAcimaDosPes = 2f;
    [Min(1f)] [SerializeField] private float distanciaMaximaAbaixoDaKaya = 700f;
    [Tooltip("Quanto o inimigo fica acima da superficie ao terminar de emergir.")]
    [SerializeField] private float alturaFinalAcimaDoChao = 0.5f;

    [Header("Equilibrio do combate")]
    [Min(1)] [SerializeField] private int maximoInvocadosVivos = 3;
    [Min(0f)] [SerializeField] private float intervaloMinimoEntreInvocacoes = 2f;
    [SerializeField] private bool mostrarPoeiraNaSuperficie = true;

    private readonly List<GameObject> invocados = new List<GameObject>();
    private float proximaInvocacao;

    private void Awake()
    {
        EncontrarKaya();
    }

    private void EncontrarKaya()
    {
        if (kaya != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) kaya = player.transform;
    }

    public void InvocarInimigoParado()
    {
        if (!isActiveAndEnabled || prefabInimigoParado == null ||
            Time.time < proximaInvocacao) return;

        EncontrarKaya();
        if (kaya == null) return;

        PlayerHealth vida = kaya.GetComponentInParent<PlayerHealth>();
        if (vida != null && vida.VidaAtual <= 0f) return;

        invocados.RemoveAll(inimigo => inimigo == null);
        if (invocados.Count >= maximoInvocadosVivos) return;

        InimigoParado modelo = prefabInimigoParado.GetComponent<InimigoParado>();
        if (modelo == null)
        {
            Debug.LogWarning("[BossPesteInvocacao] O prefab precisa ter InimigoParado no objeto raiz.", this);
            return;
        }

        float alturaPes = kaya.position.y;
        Collider2D corpoKaya = kaya.GetComponent<Collider2D>();
        if (corpoKaya != null) alturaPes = corpoKaya.bounds.min.y;

        Vector2 origem = new Vector2(kaya.position.x,
            alturaPes + inicioRaioAcimaDosPes);

        RaycastHit2D[] impactos = Physics2D.RaycastAll(
            origem, Vector2.down, distanciaMaximaAbaixoDaKaya, camadasChao);

        bool encontrouChao = false;
        Vector2 pontoChao = Vector2.zero;

        foreach (RaycastHit2D impacto in impactos)
        {
            if (impacto.collider == null || impacto.collider.isTrigger ||
                impacto.normal.y < 0.5f)
                continue;

            if (impacto.collider.transform.IsChildOf(kaya) ||
                impacto.collider.transform.IsChildOf(transform.root))
                continue;

            pontoChao = impacto.point;
            encontrouChao = true;
            break;
        }

        if (!encontrouChao)
        {
            Debug.LogWarning("[BossPesteInvocacao] Nenhum chao abaixo da Kaya. Verifique Camadas Chao.", this);
            return;
        }

        float alturaEmergir = Mathf.Max(0f, modelo.alturaEmergir);
        float enterramento = Mathf.Max(0f, alturaEmergir - alturaFinalAcimaDoChao);
        Vector3 posicaoSpawn = new Vector3(
            pontoChao.x, pontoChao.y - enterramento, prefabInimigoParado.transform.position.z);

        GameObject novoInimigo = Instantiate(prefabInimigoParado, posicaoSpawn, Quaternion.identity);
        InimigoParado inimigo = novoInimigo.GetComponent<InimigoParado>();
        if (inimigo != null)
        {
            inimigo.player = kaya;

            inimigo.raioDeteccao = Mathf.Max(
                inimigo.raioDeteccao, Vector2.Distance(kaya.position, posicaoSpawn) + 10f);

            if (mostrarPoeiraNaSuperficie && inimigo.efeitoPoeira == null)
            {
                Vector3 poeira = inimigo.offsetPoeira;
                poeira.y = enterramento + 0.5f;
                inimigo.offsetPoeira = poeira;
            }
        }

        invocados.Add(novoInimigo);
        proximaInvocacao = Time.time + intervaloMinimoEntreInvocacoes;
    }
}
