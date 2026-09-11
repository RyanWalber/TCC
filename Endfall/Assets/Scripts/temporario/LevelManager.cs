using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Referências da Cena")]
    public Transform playerTransform;
    public Transform pontoInicial;
    public Text textoMoedas;
    [HideInInspector]
    public int moedasFaseAtual = 0;
    [HideInInspector]
    public List<string> moedasColetadasTemporarias = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CarregarEstadoFase();
    }

    private void CarregarEstadoFase()
    {
        if (SaveManager.Instance == null) return;

        SaveData dados = SaveManager.Instance.dadosAtuais;

        if (dados.nomeCena != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            dados.nomeCena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            dados.temCheckpoint = false;
            dados.moedasNoCheckpoint = 0;
            dados.moedasColetadasIDs.Clear();
        }

        if (dados.temCheckpoint && playerTransform != null)
        {
            playerTransform.position = dados.posicaoCheckpoint;
        }
        else if (pontoInicial != null && playerTransform != null)
        {
            playerTransform.position = pontoInicial.position;
        }

        moedasFaseAtual = dados.moedasNoCheckpoint;
        AtualizarUIMoedas();
    }

    public void ColetarMoeda(string id)
    {
        moedasFaseAtual++;
        if (!moedasColetadasTemporarias.Contains(id))
        {
            moedasColetadasTemporarias.Add(id);
        }
        AtualizarUIMoedas();
    }

    public void AtualizarUIMoedas()
    {
        if (textoMoedas != null)
        {
            textoMoedas.text = "Moedas: " + moedasFaseAtual;
        }
    }
}