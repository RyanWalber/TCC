using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int versaoDados = 2;
    public string nomeCena = "FaseFome";
    public int faseAtual = 1;
    public bool passouCheckpoint = false;
    public bool temCheckpoint => passouCheckpoint;
    public Vector3 posicaoCheckpoint = Vector3.zero;
    public int moedasNoCheckpoint = 0;
    public int moedasAtuais = 0;
    public Vector3 posicaoSalvamento = Vector3.zero;
    public bool temPosicaoSalvamento = false;
    public List<string> moedasColetadasIDs = new List<string>();
    public List<string> moedasColetadasIDsCheckpoint = new List<string>();
    public List<string> elixiresVidaColetadosIDsCheckpoint = new List<string>();
    public List<string> elixiresVidaColetadosIDs = new List<string>();

    public float vidaKaya = 100f;
    public float furiaKaya = 0f;

    // Estado separado para que morrer restaure os atributos do último checkpoint.
    public string nomeCenaCheckpoint = "";
    public float vidaKayaCheckpoint = 100f;
    public float furiaKayaCheckpoint = 0f;
    public bool checkpointStatusRegistrado = false;

    public SaveData()
    {
        versaoDados = 2;
        nomeCena = "FaseFome";
        faseAtual = 1;
        passouCheckpoint = false;
        posicaoCheckpoint = Vector3.zero;
        moedasNoCheckpoint = 0;
        moedasAtuais = 0;
        posicaoSalvamento = Vector3.zero;
        temPosicaoSalvamento = false;
        moedasColetadasIDs = new List<string>();
        moedasColetadasIDsCheckpoint = new List<string>();
        elixiresVidaColetadosIDsCheckpoint = new List<string>();
        elixiresVidaColetadosIDs = new List<string>();

        vidaKaya = 100f;
        furiaKaya = 0f;
        nomeCenaCheckpoint = "";
        vidaKayaCheckpoint = 100f;
        furiaKayaCheckpoint = 0f;
        checkpointStatusRegistrado = false;
    }
}