using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string nomeCena = "Tutorial"; 
    public bool temCheckpoint = false;
    public Vector3 posicaoCheckpoint;
    public int moedasNoCheckpoint = 0;
    public List<string> moedasColetadasIDs = new List<string>();
}