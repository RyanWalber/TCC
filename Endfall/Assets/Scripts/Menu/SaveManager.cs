using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static SaveManager Instancia => Instance;

    public SaveData DadosAtuais = new SaveData();

    public SaveData dadosAtuais
    {
        get => DadosAtuais;
        set => DadosAtuais = value;
    }

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

    public void CriarNovoJogo()
    {
        DadosAtuais = new SaveData();
    }

    public void SalvarNoSlot(int slot)
    {
        DadosAtuais.nomeCena = SceneManager.GetActiveScene().name;
        SaveSystem.Salvar(slot, DadosAtuais);
    }

    public void SalvarSlot(int slot) => SalvarNoSlot(slot);

    public void CarregarDados(SaveData dados)
    {
        DadosAtuais = dados;
    }

    public void RegistrarCheckpoint(Vector3 posicao)
    {
        DadosAtuais.temCheckpoint = true;
        DadosAtuais.posicaoCheckpoint[0] = posicao.x;
        DadosAtuais.posicaoCheckpoint[1] = posicao.y;
        DadosAtuais.posicaoCheckpoint[2] = posicao.z;
        DadosAtuais.nomeCena = SceneManager.GetActiveScene().name;

        SaveSystem.Salvar(0, DadosAtuais);
    }
}