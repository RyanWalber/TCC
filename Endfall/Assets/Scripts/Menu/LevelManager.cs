using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private Transform playerTransform;

    [Header("Moedas na Fase")]
    [SerializeField] private List<Coin> moedasNaCena = new List<Coin>();

    public int moedasFaseAtual = 0;
    public List<string> moedasColetadasTemporarias = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (moedasNaCena.Count == 0)
        {
            moedasNaCena.AddRange(FindObjectsByType<Coin>(FindObjectsSortMode.None));
        }

        if (SaveManager.Instance == null) return;

        SaveData dados = SaveManager.Instance.DadosAtuais;

        if (dados != null)
        {
            if (dados.passouCheckpoint)
            {
                if (playerTransform != null)
                {
                    playerTransform.position = dados.posicaoCheckpoint;
                }

                moedasFaseAtual = dados.moedasNoCheckpoint;
            }

            foreach (Coin moeda in moedasNaCena)
            {
                if (moeda != null && SaveManager.Instance.MoedaJaFoiColetada(moeda.IdUnico))
                {
                    moeda.gameObject.SetActive(false);
                }
            }
        }
    }

    public void ColetarMoeda(string idMoeda)
    {
        moedasFaseAtual++;
        if (!moedasColetadasTemporarias.Contains(idMoeda))
        {
            moedasColetadasTemporarias.Add(idMoeda);
        }
    }

    public void ProcessarCheckpoint(Vector3 posicaoCentro)
    {
        if (SaveManager.Instance != null)
        {
            foreach (string id in moedasColetadasTemporarias)
            {
                SaveManager.Instance.ColetarMoeda(id);
            }

            SaveManager.Instance.RegistrarCheckpoint(posicaoCentro);
        }
    }
}