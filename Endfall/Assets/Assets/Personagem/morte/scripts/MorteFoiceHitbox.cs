using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MorteFoiceHitbox : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dano = 75f;

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
        {
            float antes = hp.VidaAtual;
            hp.TomarDanoDeInimigo(dano);
            if (hp.VidaAtual < antes)
            {
                MorteBossControllerGameplay dono = GetComponentInParent<MorteBossControllerGameplay>();
                if (dono != null) dono.AudioAoCausarHit();
            }
        }
    }
}
