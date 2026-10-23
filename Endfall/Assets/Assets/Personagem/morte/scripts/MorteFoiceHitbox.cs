using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox da foice. O controller ativa o Collider2D apenas durante os frames de acerto.
/// É anexado automaticamente ao objeto que contém o collider com 'foice' no nome.
/// </summary>
[DisallowMultipleComponent]
public class MorteFoiceHitbox : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dano = 25f;

    private Collider2D hitbox;
    private bool ativa;
    private readonly HashSet<GameObject> jogadoresAtingidos = new HashSet<GameObject>();

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (hitbox == null)
        {
            Debug.LogError("[MorteFoiceHitbox] Este componente precisa ficar no mesmo objeto que o Collider2D da foice.", this);
            enabled = false;
            return;
        }

        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    public void ConfigurarDano(float novoDano)
    {
        dano = Mathf.Max(0f, novoDano);
    }

    public void DefinirAtiva(bool valor)
    {
        if (hitbox == null) return;
        if (ativa == valor) return;

        ativa = valor;
        hitbox.enabled = valor;

        // Cada ativação do ataque representa uma janela nova de acerto.
        jogadoresAtingidos.Clear();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!ativa || outro == null) return;

        GameObject objetoJogador = null;
        if (outro.CompareTag("Player"))
            objetoJogador = outro.gameObject;
        else if (outro.transform.root.CompareTag("Player"))
            objetoJogador = outro.transform.root.gameObject;

        if (objetoJogador == null) return;

        if (!jogadoresAtingidos.Add(objetoJogador)) return;

        PlayerHealth hp = outro.GetComponentInParent<PlayerHealth>();
        if (hp == null)
            hp = objetoJogador.GetComponentInChildren<PlayerHealth>();

        if (hp != null)
            hp.TomarDanoDeInimigo(dano);
    }
}
