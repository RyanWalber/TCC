using System;
using UnityEngine;
using DragonBones;

// Feedback individual por inimigo, compativel com malhas DragonBones
// combinadas. Nao altera materials compartilhados nem as animacoes.
[Serializable]
public sealed class FeedbackDanoDragonBones
{
    [Header("Feedback ao receber dano")]
    [SerializeField] private Color corFlash = new Color(1f, 0.12f, 0.12f, 1f);
    [SerializeField, Min(0.05f)] private float duracaoFlash = 0.18f;

    [NonSerialized] private UnityArmatureComponent armadura;
    [NonSerialized] private ColorTransform corOriginal;
    [NonSerialized] private MeshRenderer[] malhas;
    [NonSerialized] private MaterialPropertyBlock[] blocosOriginais;
    [NonSerialized] private float fimFlash;
    [NonSerialized] private bool mostrandoFlash;

    public float DuracaoVisivel => Mathf.Max(0.05f, duracaoFlash);

    public void ReceberHit(MonoBehaviour dono, UnityArmatureComponent referencia)
    {
        if (dono == null) return;

        if (!mostrandoFlash)
        {
            armadura = referencia != null
                ? referencia : dono.GetComponentInChildren<UnityArmatureComponent>(true);

            // Salva as cores originais do DragonBones e os overrides
            // individuais de shader; cada inimigo tem seus proprios blocos.
            corOriginal = null;
            if (armadura != null && armadura.armature != null)
            {
                corOriginal = new ColorTransform();
                corOriginal.CopyFrom(armadura.color);
            }

            malhas = dono.GetComponentsInChildren<MeshRenderer>(true);
            blocosOriginais = new MaterialPropertyBlock[malhas.Length];
            for (int i = 0; i < malhas.Length; i++)
            {
                if (malhas[i] == null) continue;
                blocosOriginais[i] = new MaterialPropertyBlock();
                malhas[i].GetPropertyBlock(blocosOriginais[i]);
            }
        }

        fimFlash = Time.time + DuracaoVisivel;
        mostrandoFlash = true;
        AplicarFlash();
    }

    private void AplicarFlash()
    {
        if (armadura != null && armadura.armature != null)
        {
            ColorTransform vermelho = new ColorTransform();
            if (corOriginal != null) vermelho.CopyFrom(corOriginal);
            vermelho.redMultiplier *= corFlash.r;
            vermelho.greenMultiplier *= corFlash.g;
            vermelho.blueMultiplier *= corFlash.b;
            armadura.color = vermelho;
        }

        if (malhas == null) return;
        for (int i = 0; i < malhas.Length; i++)
        {
            MeshRenderer renderizador = malhas[i];
            if (renderizador == null) continue;
            MaterialPropertyBlock bloco = new MaterialPropertyBlock();
            renderizador.GetPropertyBlock(bloco);
            bloco.SetColor("_Color", corFlash);
            renderizador.SetPropertyBlock(bloco);
        }
    }

    public void Atualizar()
    {
        if (mostrandoFlash && Time.time >= fimFlash)
            Restaurar();
    }

    public void Restaurar()
    {
        if (!mostrandoFlash) return;
        mostrandoFlash = false;

        if (armadura != null && armadura.armature != null && corOriginal != null)
            armadura.color = corOriginal;

        if (malhas != null && blocosOriginais != null)
        {
            for (int i = 0; i < malhas.Length; i++)
                if (malhas[i] != null)
                    malhas[i].SetPropertyBlock(blocosOriginais[i]);
        }

        malhas = null;
        blocosOriginais = null;
        corOriginal = null;
    }
}
