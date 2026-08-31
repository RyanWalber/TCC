using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string CHAVE_CRIPTOGRAFIA = "ChaveSecretaTrabalhoGame123";
    public SaveData dadosAtuais = new SaveData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NovoJogo()
    {
        dadosAtuais = new SaveData();
    }

    public void SalvarSlot(int slot)
    {
        string json = JsonUtility.ToJson(dadosAtuais, true);
        byte[] bytesOriginais = Encoding.UTF8.GetBytes(json);
        byte[] bytesEncriptados = Criptografar(bytesOriginais);

        File.WriteAllBytes(ObterCaminho(slot), bytesEncriptados);

        if (slot != 0)
        {
            File.WriteAllBytes(ObterCaminho(0), bytesEncriptados);
        }
    }

    public bool CarregarSlot(int slot)
    {
        string caminho = ObterCaminho(slot);
        if (!File.Exists(caminho)) return false;

        byte[] bytesEncriptados = File.ReadAllBytes(caminho);
        byte[] bytesDecriptados = Criptografar(bytesEncriptados);
        string json = Encoding.UTF8.GetString(bytesDecriptados);

        dadosAtuais = JsonUtility.FromJson<SaveData>(json);

        if (slot != 0)
        {
            SalvarSlot(0);
        }

        return true;
    }

    public void CarregarEIniciarJogo(int slot)
    {
        if (CarregarSlot(slot))
        {
            SceneManager.LoadScene(dadosAtuais.nomeCena);
        }
    }

    public string ObterCaminho(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.dat");
    }

    private byte[] Criptografar(byte[] dados)
    {
        byte[] chaveBytes = Encoding.UTF8.GetBytes(CHAVE_CRIPTOGRAFIA);
        byte[] resultado = new byte[dados.Length];

        for (int i = 0; i < dados.Length; i++)
        {
            resultado[i] = (byte)(dados[i] ^ chaveBytes[i % chaveBytes.Length]);
        }

        return resultado;
    }
}