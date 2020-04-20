using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour
{
    public AudioSource m_AudioSource_Effects;
    public AudioSource m_AudioSource_Music;

    public int Score { get; set; }

    public int GemsTotal { get; set; }

    public int GemsCollected { get; set; }

    public int LevelIndex { get; set; }

    List<string> m_levels;

    private void Start()
    {
        m_levels = new List<string>()
        {
            "level-01",
            "level-02",
            "level-03",
            "level-04",
            "level-05",
            "level-06",
            "level-07",
            "level-08"
        };

        Reset();
    }

    public void Reset()
    {
        Score = 0;
        GemsTotal = 0;
        GemsCollected = 0;
        LevelIndex = 1;
    }

    void OnLevelWasLoaded(int level)
    {
        GemsTotal += FindObjectsOfType<Collectable>().Length;  
    }

    public void LoadNextLevel()
    {
        LevelIndex++;

        if (LevelIndex > m_levels.Count)
        {
            StartCoroutine(GameData.LoadScene("ending"));
        }
        else
        {
            StartCoroutine(LoadScene(m_levels[LevelIndex - 1]));
        }
    }

#if UNITY_EDITOR 
    private void Awake()
    {
        if (PreloadIntegration.otherScene > 0)
        {
            Debug.Log("Returning again to the scene: " + PreloadIntegration.otherScene);
            SceneManager.LoadScene(PreloadIntegration.otherScene);
        }
        else
        {
            StartCoroutine(LoadScene("menu"));
        }
    }
#else
    private void Awake()
    {
        StartCoroutine(LoadScene("menu"));      
    }
#endif

    public static IEnumerator LoadScene(string scene)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(scene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}

public class PreloadIntegration
{
#if UNITY_EDITOR
    public static int otherScene = -2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preload()
    {
        Debug.Log("InitLoadingScene()");

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == 0)
        {
            return;
        }

        Debug.Log("Loading _preload scene");
        otherScene = sceneIndex;

        SceneManager.LoadScene(0);
    }
#endif
}