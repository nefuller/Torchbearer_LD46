using System.Globalization;
using TMPro;
using UnityEngine;

public class EndingController : MonoBehaviour
{
    public TextMeshProUGUI m_Text_Score;
    public TextMeshProUGUI m_Text_Percentage;

    float m_timeout = 1.5f;

    void Start()
    {
        var gameData = FindObjectOfType<GameData>();
        m_Text_Score.text = gameData.Score.ToString();

        m_Text_Percentage.text = string.Format(CultureInfo.InvariantCulture, "You collected {0:n0}% of gemstones", 100f / gameData.GemsTotal * gameData.GemsCollected);
    }

    void Update()
    {
        m_timeout -= Time.deltaTime;

        if (Input.anyKeyDown && m_timeout < 0)
        {
            StartCoroutine(GameData.LoadScene("menu"));
        }
    }
}
