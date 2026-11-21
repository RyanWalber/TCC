using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CursorManagerGlobal : MonoBehaviour
{
    public static CursorManagerGlobal Instance { get; private set; }

    private Texture2D cursorMenu;
    private Texture2D cursorMira;
    private Vector2 hotspotMenu = Vector2.zero;
    private Vector2 offsetHotspotMira = Vector2.zero;
    private bool estaPausado;
    private bool temFoco = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void IniciarNasCenas()
    {
        Garantir();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparEstadoDePlayAnterior()
    {
        Instance = null;
    }

    public static CursorManagerGlobal Garantir()
    {
        if (Instance != null) return Instance;

        CursorManagerGlobal existente = FindFirstObjectByType<CursorManagerGlobal>();
        if (existente != null) return existente;

        GameObject controlador = new GameObject("CursorGlobalEndFall");
        return controlador.AddComponent<CursorManagerGlobal>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CarregarRecursosSeExistirem();
        AplicarCursor();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    public void Configurar(Texture2D setaDoMenu, Texture2D miraDoJogo, Vector2 pontaDaSeta)
    {
        Configurar(setaDoMenu, miraDoJogo, pontaDaSeta, Vector2.zero);
    }

    public void Configurar(Texture2D setaDoMenu, Texture2D miraDoJogo, Vector2 pontaDaSeta, Vector2 offsetMira)
    {
        if (setaDoMenu != null) cursorMenu = setaDoMenu;
        if (miraDoJogo != null) cursorMira = miraDoJogo;
        hotspotMenu = pontaDaSeta;
        offsetHotspotMira = offsetMira;
        CarregarRecursosSeExistirem();
        AplicarCursor();
    }

    private void CarregarRecursosSeExistirem()
    {
        if (cursorMenu == null)
            cursorMenu = Resources.Load<Texture2D>("Cursores/CursorMenu");

        if (cursorMira == null)
            cursorMira = Resources.Load<Texture2D>("Cursores/CursorMira");
    }

    private static bool EhCenaDeJogo(string nomeDaCena)
    {
        return nomeDaCena.StartsWith("Fase", StringComparison.OrdinalIgnoreCase)
            || nomeDaCena.Equals("Tutorial", StringComparison.OrdinalIgnoreCase);
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        estaPausado = false;
        AplicarCursor();
    }

    public void DefinirModoPausa(bool pausado)
    {
        estaPausado = pausado;
        AplicarCursor();
    }

    private void AplicarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!temFoco)
        {
            RestaurarCursorDoWindows();
            return;
        }

        bool mostrarMira = EhCenaDeJogo(SceneManager.GetActiveScene().name) && !estaPausado;
        Texture2D textura = mostrarMira ? cursorMira : cursorMenu;

        Vector2 hotspot = Vector2.zero;
        if (textura != null)
        {
            hotspot = mostrarMira
                ? new Vector2(textura.width * 0.5f, textura.height * 0.5f) + offsetHotspotMira
                : hotspotMenu;

            hotspot.x = Mathf.Clamp(hotspot.x, 0f, Mathf.Max(0f, textura.width - 1));
            hotspot.y = Mathf.Clamp(hotspot.y, 0f, Mathf.Max(0f, textura.height - 1));
        }

        Cursor.SetCursor(textura, hotspot, CursorMode.Auto);
    }

    private static void RestaurarCursorDoWindows()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnApplicationFocus(bool focado)
    {
        temFoco = focado;
        if (focado) AplicarCursor();
        else RestaurarCursorDoWindows();
    }

    private void OnApplicationQuit()
    {
        RestaurarCursorDoWindows();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        RestaurarCursorDoWindows();
    }
}
