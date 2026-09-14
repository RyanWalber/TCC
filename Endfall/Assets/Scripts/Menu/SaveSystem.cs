using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string ChaveCripto = "ChaveCriptoEndfall2026";

    private static string ObterCaminhoSlot(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
    }

    public static void Salvar(int slot, SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        byte[] dadosCriptografados = CriptografarXOR(Encoding.UTF8.GetBytes(json));
        File.WriteAllBytes(ObterCaminhoSlot(slot), dadosCriptografados);

        if (slot != 0)
        {
            File.WriteAllBytes(ObterCaminhoSlot(0), dadosCriptografados);
        }
    }

    public static SaveData Carregar(int slot)
    {
        string caminho = ObterCaminhoSlot(slot);
        if (!File.Exists(caminho)) return null;

        byte[] dadosBytes = File.ReadAllBytes(caminho);
        string json = Encoding.UTF8.GetString(CriptografarXOR(dadosBytes));
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (slot != 0 && data != null)
        {
            Salvar(0, data);
        }

        return data;
    }

    public static bool SlotExiste(int slot)
    {
        return File.Exists(ObterCaminhoSlot(slot));
    }

    private static byte[] CriptografarXOR(byte[] input)
    {
        byte[] chaveBytes = Encoding.UTF8.GetBytes(ChaveCripto);
        byte[] resultado = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            resultado[i] = (byte)(input[i] ^ chaveBytes[i % chaveBytes.Length]);
        }
        return resultado;
    }
}