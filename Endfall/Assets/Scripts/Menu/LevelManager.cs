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
            moedasNaCena.AddRange(
                FindObjectsByType<Coin>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            );
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegistrarTotalMoedasFase(moedasNaCena.Count);
        }

        if (SaveManager.Instance == null) return;

        SaveData dados = SaveManager.Instance.DadosAtuais;

        if (dados != null)
        {
            moedasFaseAtual = SaveManager.Instance.GetMoedasAtuais();

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
        if (string.IsNullOrEmpty(idMoeda))
        {
            return;
        }

        if (!moedasColetadasTemporarias.Contains(idMoeda))
        {
            moedasColetadasTemporarias.Add(idMoeda);
            moedasFaseAtual++;
        }
    }

    public void RegistrarColetaveisPendentesNoSave()
    {
        if (SaveManager.Instance == null) return;

        foreach (string id in moedasColetadasTemporarias)
            SaveManager.Instance.ColetarMoeda(id);

        moedasColetadasTemporarias.Clear();
        moedasFaseAtual = SaveManager.Instance.GetMoedasAtuais();
    }

    public void ProcessarCheckpoint(Vector3 posicaoCentro)
    {
        if (SaveManager.Instance != null)
        {
            RegistrarColetaveisPendentesNoSave();
            SaveManager.Instance.RegistrarCheckpoint(posicaoCentro);
        }
    }
}