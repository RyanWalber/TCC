using UnityEngine;

// Toca um som do proprio objeto ate o fim, mesmo que o coletavel seja desativado.
public static class AudioObjeto2D
{
    public static void Tocar(AudioClip clip, float volume, Vector3 posicao, string nome)
    {
        if (clip == null || volume <= 0f) return;
        GameObject objeto = new GameObject("Audio - " + nome);
        objeto.transform.position = posicao;
        AudioSource fonte = objeto.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(clip, Mathf.Clamp01(volume));
        Object.Destroy(objeto, Mathf.Max(0.1f, clip.length) + 0.2f);
    }
}
