using System.Collections;
using UnityEngine;

public class SpikeController : MonoBehaviour
{
    public Transform m_Transform_Spikes;
    public ParticleSystem m_Particles_Blood;
    public AudioClip m_AudioClip_Spikes;

    GameData m_gameData;

    void Start()
    {
        m_gameData = FindObjectOfType<GameData>();
    }

    public void Trigger()
    {
        StartCoroutine(AnimateTrap());
    }

    IEnumerator AnimateTrap()
    {
        var delta = 0f;
        var duration = .2f;

        var start = m_Transform_Spikes.position;
        var end = m_Transform_Spikes.position + new Vector3(0, 0.93f, 0);

        m_gameData.m_AudioSource_Effects.PlayOneShot(m_AudioClip_Spikes);

        m_Particles_Blood.gameObject.SetActive(true);

        while (delta <= duration)
        {
            delta += Time.deltaTime;

            m_Transform_Spikes.position = Vector3.Lerp(start, end, delta / duration);

            yield return null;
        }

        m_Transform_Spikes.position = end;
    }
}
