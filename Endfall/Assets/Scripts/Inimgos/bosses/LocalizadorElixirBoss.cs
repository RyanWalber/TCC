using System.Collections.Generic;
using UnityEngine;

// Sistema compartilhado entre os BossVida (Fome/Peste/Morte) e VidaBossGuerra.
// Escolhe um elixir visivel, distante da Kaya e acima de plataforma real.
public static class LocalizadorElixirBoss
{
    public static bool Escolher(
        Transform kaya, float zDoElixir, LayerMask camadaChao,
        float alturaAcimaChao, int pontos, float margemX, float margemY,
        float distanciaMinimaNormalizada, float variacaoMaximaAltura,
        float distanciaEntreElixiresNormalizada,
        List<HealthPickup> existentes, out Vector3 escolhido)
    {
        escolhido = Vector3.zero;
        if (kaya == null) return false;

        Camera cameraJogo = Camera.main;
        if (cameraJogo == null || !cameraJogo.isActiveAndEnabled) return false;

        float profundidade = Vector3.Dot(
            kaya.position - cameraJogo.transform.position,
            cameraJogo.transform.forward);
        if (profundidade <= 0f) return false;

        margemX = Mathf.Clamp(margemX, 0.03f, 0.25f);
        margemY = Mathf.Clamp(margemY, 0.03f, 0.25f);

        Vector3 extremoEsquerdo = cameraJogo.ViewportToWorldPoint(
            new Vector3(margemX, 0.5f, profundidade));
        Vector3 extremoDireito = cameraJogo.ViewportToWorldPoint(
            new Vector3(1f - margemX, 0.5f, profundidade));
        float largura = Mathf.Abs(extremoDireito.x - extremoEsquerdo.x);
        if (largura < 0.1f) return false;

        float distanciaMinima = largura * Mathf.Clamp(
            distanciaMinimaNormalizada, 0.1f, 0.75f);
        float afastamentoElixires = largura * Mathf.Clamp(
            distanciaEntreElixiresNormalizada, 0.05f, 0.5f);

        // Encontra o chao onde Kaya esta, mas sem assumir que um Tilemap
        // inteiro e a mesma plataforma.
        Collider2D pisoKaya = null;
        float alturaPisoKaya = kaya.position.y;
        RaycastHit2D[] sobKaya = Physics2D.RaycastAll(
            new Vector2(kaya.position.x, kaya.position.y + 1f),
            Vector2.down, 160f, camadaChao);
        foreach (RaycastHit2D hit in sobKaya)
        {
            if (hit.collider == null || hit.collider.isTrigger ||
                hit.normal.y < 0.55f) continue;
            pisoKaya = hit.collider;
            alturaPisoKaya = hit.point.y;
            break;
        }

        float melhorNota = float.NegativeInfinity;
        bool achou = false;
        int tentativas = Mathf.Clamp(pontos, 4, 12);

        for (int i = 0; i < tentativas; i++)
        {
            float xTela = Mathf.Lerp(margemX, 1f - margemX,
                (i + 0.5f) / tentativas);
            Vector3 topo = cameraJogo.ViewportToWorldPoint(
                new Vector3(xTela, 1f - margemY, profundidade));
            Vector3 baseVisivel = cameraJogo.ViewportToWorldPoint(
                new Vector3(xTela, margemY, profundidade));
            float ySuperior = Mathf.Max(topo.y, baseVisivel.y);
            float yInferior = Mathf.Min(topo.y, baseVisivel.y);
            if (ySuperior <= yInferior) continue;

            RaycastHit2D[] plataformas = Physics2D.RaycastAll(
                new Vector2(topo.x, ySuperior), Vector2.down,
                ySuperior - yInferior, camadaChao);
            foreach (RaycastHit2D hit in plataformas)
            {
                if (hit.collider == null || hit.collider.isTrigger ||
                    hit.normal.y < 0.55f) continue;

                Vector3 candidato = new Vector3(hit.point.x,
                    hit.point.y + Mathf.Max(0f, alturaAcimaChao), zDoElixir);
                Vector3 viewport = cameraJogo.WorldToViewportPoint(candidato);
                if (viewport.z <= 0f ||
                    viewport.x < margemX || viewport.x > 1f - margemX ||
                    viewport.y < margemY || viewport.y > 1f - margemY)
                    continue;

                float distanciaX = Mathf.Abs(candidato.x - kaya.position.x);
                if (distanciaX < distanciaMinima) continue;

                float diferencaAltura = Mathf.Abs(hit.point.y - alturaPisoKaya);
                if (diferencaAltura > Mathf.Max(0f, variacaoMaximaAltura))
                    continue;

                bool proximoDeOutro = false;
                if (existentes != null)
                {
                    foreach (HealthPickup elixir in existentes)
                    {
                        if (elixir != null && elixir.gameObject.activeInHierarchy &&
                            Vector2.Distance(elixir.transform.position, candidato) <
                            afastamentoElixires)
                        {
                            proximoDeOutro = true;
                            break;
                        }
                    }
                }
                if (proximoDeOutro) continue;

                bool mesmoPiso = pisoKaya != null && hit.collider == pisoKaya;
                float nota = distanciaX - diferencaAltura * 0.25f +
                    (mesmoPiso ? largura * 2f : 0f);
                if (nota > melhorNota)
                {
                    melhorNota = nota;
                    escolhido = candidato;
                    achou = true;
                }
            }
        }
        return achou;
    }
}
