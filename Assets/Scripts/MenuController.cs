using UnityEngine;

public class MenuController : MonoBehaviour
{
    private void Start()
    {
        var gameData = FindObjectOfType<GameData>();
        gameData.Reset();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        else if (Input.anyKeyDown)
        {
            StartCoroutine(GameData.LoadScene("level-01"));
        }
    }
}
