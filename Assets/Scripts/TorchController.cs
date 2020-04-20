using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OxygenVolume
{
    public Vector2Int Position;
    public float Oxygen;
    public bool IsOccupied;
    public bool IsProcessed;
}

public class TorchController : MonoBehaviour
{
    public Light m_Light_Torch;
    public Light m_Light_PointLight;
    public ParticleSystem m_Particles_Torch;


    public int m_NumGridSquares = 64;
    public float m_OxygenPerGridSquare = 1f;
    public float m_OxygenConsumptionRate = 0.01f;

    public event EventHandler TorchIsDeadEvent;

    bool m_torchIsDeadEventWasFired = false;
    PlayerController m_playerController;
    Dictionary<Vector2Int, OxygenVolume> m_oxygenVolumes;
    List<OxygenVolume> m_activeVolumes;

    void Start()
    {
        m_playerController = FindObjectOfType<PlayerController>();
        m_playerController.LevelLayoutChangedEvent += OnLevelLayoutChangedEvent;

        m_oxygenVolumes = InitializeOxygenVolumes();
        m_activeVolumes = GetActiveVolumesForPositionAndLayout(m_playerController.transform.position);

        m_Light_Torch.intensity = 1f;
    }

    private void OnLevelLayoutChangedEvent(object sender, EventArgs e)
    {
        m_activeVolumes = GetActiveVolumesForPositionAndLayout(m_playerController.transform.position);
    }

    void OnDestroy()
    {
        if (m_playerController != null)
        {
            m_playerController.LevelLayoutChangedEvent -= OnLevelLayoutChangedEvent;
        }
    }

    void Update()
    {
        if (!m_torchIsDeadEventWasFired && m_playerController.m_IsLevelStarted)
        {
            var oxygen = UpdateOxygenLevel(m_activeVolumes);
            

            var light = oxygen.Remap(0, 6, 0f, 2f);
            if (light <= 0.1f)
            {
                TorchIsDeadEvent(this, null);
                m_torchIsDeadEventWasFired = true;
            }
            else
            {
                m_Light_Torch.intensity = Mathf.Clamp(light, 0, 2);
                m_Light_PointLight.intensity = Mathf.Clamp(light, 0, 2);
                RenderSettings.ambientIntensity = Mathf.Clamp(light, 0, 2);
                m_Particles_Torch.startLifetime = Mathf.Clamp(light.Remap(0, 2, 0f, 1f), 0, 1);
            }
        }
    }

    public void UpdatePushablePosition(Vector3 start, Vector3 end)
    {
        var startKey = ConvertToVector2Int(start);
        var endKey = ConvertToVector2Int(end);

        m_oxygenVolumes[startKey].IsOccupied = false;
        m_oxygenVolumes[endKey].IsOccupied = true;
    }

    Vector2Int ConvertToVector2Int(Vector3 position)
    {
        // -3.5 = 0, -2.5 = 1, -1.5 = 2, -0.5 = 3, +0.5 = 4, +1.5 = 5, +2.5 = 6, +3.5 = 7

        var x = (int)position.x.Remap(-3.5f, 3.5f, 0, 7);
        var z = (int)position.z.Remap(-3.5f, 3.5f, 0, 7);

        return new Vector2Int(x, z);
    }

    Dictionary<Vector2Int, OxygenVolume> InitializeOxygenVolumes()
    {
        var AXIS_HALF_LENGTH = 3.5f;
        var AXIS_LENGTH_IN_GRID_UNITS = 8;
        var startX = -AXIS_HALF_LENGTH;
        var startZ = -AXIS_HALF_LENGTH;
        var offset = 1f;

        var volumes = new Dictionary<Vector2Int, OxygenVolume>();

        var currX = startX;
        var currZ = startZ;

        for (var x = 0; x < AXIS_LENGTH_IN_GRID_UNITS; x++)
        {
            for (var z = 0; z < AXIS_LENGTH_IN_GRID_UNITS; z++)
            {
                var pos = new Vector3(currX, 0.5f, currZ);
                var posInt = ConvertToVector2Int(pos);

                var ray = new Ray(pos + PlayerController.RAY_OFFSET, Vector3.down);
                
                var hit = Physics.Raycast(ray, out RaycastHit hitInfo, 100f);
                if (hit)
                {
                    if (hitInfo.transform.gameObject.layer == Layers.LAYER_FLOOR ||
                        hitInfo.transform.gameObject.layer == Layers.LAYER_COLLECTABLE ||
                        hitInfo.transform.gameObject.layer == Layers.LAYER_TRIGGER ||
                        hitInfo.transform.gameObject.layer == Layers.LAYER_TRAP)
                    {
                        volumes.Add(posInt, new OxygenVolume() { Position = posInt, Oxygen = m_OxygenPerGridSquare, IsOccupied = false });
                    }
                    else if (hitInfo.transform.gameObject.layer == Layers.LAYER_PUSHABLE)
                    {
                        volumes.Add(posInt, new OxygenVolume() { Position = posInt, Oxygen = 0f, IsOccupied = true });
                    }
                }

                currZ += offset;
            }

            if (x < AXIS_LENGTH_IN_GRID_UNITS - 1)
            {
                currX += offset;    
            } 
            else
            {
                currX = startX;
            }

            currZ = startZ;
        }

        return volumes;
    }

    float UpdateOxygenLevel(List<OxygenVolume> volumes)
    {
        var oxygen = 0f;

        //var consumptionPerVolume = m_OxygenConsumptionRate / volumes.Count;

        foreach (var volume in volumes)
        {
            if (volume.Oxygen > 0)
            {
                volume.Oxygen -= m_OxygenConsumptionRate * Time.deltaTime;
                volume.Oxygen = Mathf.Clamp(volume.Oxygen, 0, 100f);
                oxygen += volume.Oxygen;
            }
        }

        return oxygen;
    }

    List<OxygenVolume> GetActiveVolumesForPositionAndLayout(Vector3 playerPosition)
    {
        foreach (var volume in m_oxygenVolumes)
        {
            volume.Value.IsProcessed = false;
        }

        var nodes = new Queue<Vector2Int>();
        nodes.Enqueue(ConvertToVector2Int(playerPosition));

        var volumes = new List<OxygenVolume>();
        var isFirstNode = true;

        while (nodes.Count > 0)
        {
            if (nodes.Count > 64)
            {
                break;
            }

            var node = nodes.Dequeue();

            if (m_oxygenVolumes.TryGetValue(node, out OxygenVolume value))
            {              
                if (isFirstNode)
                {
                    isFirstNode = false;
                    value.IsProcessed = true;
                }

                volumes.Add(value);                    
            }

            if (m_oxygenVolumes.TryGetValue(node + new Vector2Int(0, 1), out OxygenVolume topVolume))
            {
                if (!topVolume.IsProcessed && !topVolume.IsOccupied)
                {
                    topVolume.IsProcessed = true;

                    nodes.Enqueue(topVolume.Position);
                }
            }

            if (m_oxygenVolumes.TryGetValue(node + new Vector2Int(0, -1), out OxygenVolume bottomVolume))
            {
                if (!bottomVolume.IsProcessed && !bottomVolume.IsOccupied)
                {
                    bottomVolume.IsProcessed = true;

                    nodes.Enqueue(bottomVolume.Position);
                }
            }

            if (m_oxygenVolumes.TryGetValue(node + new Vector2Int(-1, 0), out OxygenVolume leftVolume))
            {
                if (!leftVolume.IsProcessed && !leftVolume.IsOccupied)
                {
                    leftVolume.IsProcessed = true;

                    nodes.Enqueue(leftVolume.Position);
                }
            }

            if (m_oxygenVolumes.TryGetValue(node + new Vector2Int(1, 0), out OxygenVolume rightVolume))
            {
                if (!rightVolume.IsProcessed && !rightVolume.IsOccupied)
                {
                    rightVolume.IsProcessed = true;

                    nodes.Enqueue(rightVolume.Position);
                }
            }
        }

        return volumes;
    }
}
