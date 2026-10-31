using UnityEngine;

// Preferencias locais do jogador; independentes dos slots de save.
public static class ConfiguracoesJogo
{
    private const string ChaveVolume = "EndFall.Config.VolumeGeral";
    private const string ChaveMusica = "EndFall.Config.MusicaAtiva";
    private const string ChaveMira = "EndFall.Config.SensibilidadeMira";

    public static float VolumeGeral
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolume, 1f));
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveVolume, volume);
            AudioListener.volume = volume;
            PlayerPrefs.Save();
        }
    }

    public static bool MusicaAtiva
    {
        get => PlayerPrefs.GetInt(ChaveMusica, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ChaveMusica, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // No EndFall 2D, influencia a resposta da mira, nao o cursor do Windows.
    public static float SensibilidadeMira
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(ChaveMira, 1f), 0.5f, 2f);
        set
        {
            PlayerPrefs.SetFloat(ChaveMira, Mathf.Clamp(value, 0.5f, 2f));
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AplicarVolumeSalvo()
    {
        AudioListener.volume = VolumeGeral;
    }
}
