using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float m_MovementDistance = 1f;
    public float m_MovementSpeed = 0.25f;
    public GameObject m_Prefab_Wall;
    public Transform m_Transform_ExternalWalls;
    public Transform m_Transform_Door;
    public bool m_IsLevelStarted = false;

    public AudioClip m_AudioClip_OpenDoor;
    public AudioClip m_AudioClip_Footsteps;
    public AudioClip m_AudioClip_Collectable;
    public TextMeshProUGUI m_Text_Dead;
    public TextMeshProUGUI m_Text_Continue;
    public TextMeshProUGUI m_Text_Score;

    public event EventHandler LevelLayoutChangedEvent;

    public static readonly Vector3 RAY_OFFSET = new Vector3(0, 5f, 0);
    public static readonly float RAY_DISTANCE = 50f;

    static readonly Vector3 INGRESS_DOOR_POSITION = new Vector3(-4.5f, 0.5f, 2.5f);

    Vector3 m_horzMovement;
    Vector3 m_vertMovement;
    bool m_isMoving = false;
    TorchController m_torchController;
    bool m_isWaitingForKeypress = false;
    GameData m_gameData;
    float m_resetTimeout = 1.5f;
    float m_moveTimeout;
    const float MOVE_TIMEOUT = .15f;
    void Start()
    {
        m_horzMovement = new Vector3(m_MovementDistance, 0, 0);
        m_vertMovement = new Vector3(0, 0, m_MovementDistance);

        m_torchController = FindObjectOfType<TorchController>();
        m_torchController.TorchIsDeadEvent += OnTorchIsDeadEvent;

        m_gameData = FindObjectOfType<GameData>();

        m_Text_Score.text = string.Format(CultureInfo.InvariantCulture, "Score: {0}", m_gameData.Score);

        m_moveTimeout = MOVE_TIMEOUT;
    }

    void OnDestroy()
    {
        if (m_torchController != null)
        { 
            m_torchController.TorchIsDeadEvent -= OnTorchIsDeadEvent;
        }
    }

    private void OnTorchIsDeadEvent(object sender, System.EventArgs e)
    {
        m_Text_Dead.gameObject.SetActive(true);
        m_Text_Continue.gameObject.SetActive(true);

        m_isWaitingForKeypress = true;
    }

    void Update()
    {
        if (m_isWaitingForKeypress)
        {
            m_resetTimeout -= Time.deltaTime;

            if (Input.anyKeyDown && m_resetTimeout < 0)
            {
                StartCoroutine(GameData.LoadScene("menu"));
            }

            return;
        }

        m_moveTimeout -= Time.deltaTime;
        if (m_isMoving || m_moveTimeout > 0f)
        {
            return;
        }

        var horz = Input.GetAxis("Horizontal");
        var vert = Input.GetAxis("Vertical");

        var left = Input.GetKey(KeyCode.A);
        var right = Input.GetKey(KeyCode.D);
        var up = Input.GetKey(KeyCode.W);
        var down = Input.GetKey(KeyCode.S);

        if (left)
        {
            StartCoroutine(TryMove(transform.position - m_horzMovement));
        }
        else if (right)
        {
            StartCoroutine(TryMove(transform.position + m_horzMovement));
        }
        else if (up)
        {
            StartCoroutine(TryMove(transform.position + m_vertMovement));
        }
        else if (down)
        {
            StartCoroutine(TryMove(transform.position - m_vertMovement));
        }
    }

    IEnumerator TryMove(Vector3 nextPlayerPosition)
    {
        m_isMoving = true;
        m_moveTimeout = MOVE_TIMEOUT;

        var ray = new Ray(nextPlayerPosition + RAY_OFFSET, Vector3.down);

        var hit = Physics.Raycast(ray, out RaycastHit hitInfo, RAY_DISTANCE);
        if (hit)
        {
            
            Debug.Log(hitInfo.transform.gameObject.name);
            if (hitInfo.transform.gameObject.layer == Layers.LAYER_FLOOR ||
                hitInfo.transform.gameObject.layer == Layers.LAYER_TRIGGER)
            {
             
                yield return StartCoroutine(Move(nextPlayerPosition));
            }
            else if (hitInfo.transform.gameObject.layer == Layers.LAYER_COLLECTABLE)
            {
    
                if (hitInfo.transform.gameObject.name == "Key")
                {
                    yield return StartCoroutine(AddKey(hitInfo.transform.gameObject));
                }
                else
                {
                    yield return StartCoroutine(AddGemstone(hitInfo.transform.gameObject));
                }

                yield return StartCoroutine(Move(nextPlayerPosition));

            }
            else if (hitInfo.transform.gameObject.layer == Layers.LAYER_PUSHABLE)
            {
                if (CheckIfPushableCanBePushed(nextPlayerPosition, hitInfo.transform))
                {
                    StartCoroutine(MovePushable(nextPlayerPosition, hitInfo.transform));

                    yield return StartCoroutine(Move(nextPlayerPosition));
                }
            }
            else if (hitInfo.transform.gameObject.layer == Layers.LAYER_DOOR)
            {
                if (m_HasKey)
                {
                    StartCoroutine(OpenDoor());

                    yield return StartCoroutine(Move(nextPlayerPosition));
                }
            }
            else if (hitInfo.transform.gameObject.layer == Layers.LAYER_TRAP)
            {
                yield return StartCoroutine(Move(nextPlayerPosition));

                hitInfo.transform.GetComponent<SpikeController>().Trigger();

                OnTorchIsDeadEvent(this, null);
            }
        }

        m_isMoving = false;
    }

    bool m_isDoorOpen = false;
    IEnumerator OpenDoor()
    {
        if (m_isDoorOpen)
        {
            yield break;
        }

        if (!m_HasKey)
        {
            yield break;
        }

        m_isDoorOpen = true;

        var delta = 0f;
        var duration = m_MovementSpeed / 2f;

        var start = m_Transform_Door.rotation;
        var end = Quaternion.AngleAxis(90f, Vector3.up);

        m_gameData.m_AudioSource_Effects.PlayOneShot(m_AudioClip_OpenDoor);

        while (delta <= duration)
        {
            delta += Time.deltaTime;

            m_Transform_Door.rotation = Quaternion.Lerp(start, end, delta / duration);

            yield return null;
        }

        m_Transform_Door.rotation = end;
    }

    IEnumerator Move(Vector3 nextPlayerPosition)
    {
        var delta = 0f;
        var duration = m_MovementSpeed;

        var start = transform.position;

        m_gameData.m_AudioSource_Effects.PlayOneShot(m_AudioClip_Footsteps, 0.5f);

        while (delta <= duration)
        {
            delta += Time.deltaTime;

            transform.position = Vector3.Lerp(start, nextPlayerPosition, delta / duration);

            yield return null;
        }

        transform.position = nextPlayerPosition;

        var ray = new Ray(transform.position + RAY_OFFSET, Vector3.down);

        var hit = Physics.Raycast(ray, out RaycastHit hitInfo, RAY_DISTANCE);
        if (hit && hitInfo.transform.gameObject.layer == Layers.LAYER_TRAP)
        {
            hitInfo.transform.GetComponent<SpikeController>().Trigger();

            OnTorchIsDeadEvent(this, null);
        }
    }

    bool CheckIfPushableCanBePushed(Vector3 nextPlayerPosition, Transform pushableTransform)
    {
        var dir = (nextPlayerPosition - transform.position).normalized;

        var ray = new Ray(pushableTransform.position + (dir * m_MovementDistance) + RAY_OFFSET, Vector3.down);

        var hit = Physics.Raycast(ray, out RaycastHit hitInfo, RAY_DISTANCE);
        if (hit)
        {
            if (hitInfo.transform.gameObject.layer == Layers.LAYER_FLOOR ||
                hitInfo.transform.gameObject.layer == Layers.LAYER_COLLECTABLE ||
                hitInfo.transform.gameObject.layer == Layers.LAYER_TRIGGER ||
                hitInfo.transform.gameObject.layer == Layers.LAYER_TRAP)
            {
                return true;
            }
        }

        return false;
    }

    IEnumerator MovePushable(Vector3 nextPlayerPosition, Transform pushableTransfrom)
    {
        var movementDir = (nextPlayerPosition - transform.position).normalized;

        var delta = 0f;
        var duration = m_MovementSpeed;

        var start = pushableTransfrom.position;
        var end = start + (movementDir * m_MovementDistance);

        while (delta <= duration)
        {
            delta += Time.deltaTime;

            pushableTransfrom.position = Vector3.Lerp(start, end, delta / duration);

            yield return null;
        }

        pushableTransfrom.position = end;

        m_torchController.UpdatePushablePosition(start, end);

        LevelLayoutChangedEvent.Invoke(this, null);
    }

    IEnumerator AddGemstone(GameObject gemstone)
    {
        Destroy(gemstone);

        m_gameData.m_AudioSource_Effects.PlayOneShot(m_AudioClip_Collectable, 0.45f);

        m_gameData.Score += 100;
        m_gameData.GemsCollected += 1;

        m_Text_Score.text = string.Format(CultureInfo.InvariantCulture, "Score: {0}", m_gameData.Score);

        yield return null;
    }

    public bool m_HasKey = false;

    IEnumerator AddKey(GameObject key)
    {
        Destroy(key);

        m_gameData.m_AudioSource_Effects.PlayOneShot(m_AudioClip_Collectable, 0.45f);

        m_HasKey = true;

        yield return null;
    }

    void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.name)
        {
            case "IngressTrigger":
                StartCoroutine(StartLevel());

                LevelLayoutChangedEvent.Invoke(this, null);

                break;
            case "EgressTrigger":
                m_gameData.LoadNextLevel();

                break;
        }
    }

   IEnumerator StartLevel()
    {
        Instantiate(m_Prefab_Wall, INGRESS_DOOR_POSITION, Quaternion.identity, m_Transform_ExternalWalls);

        m_IsLevelStarted = true;

        yield return null;
    }
}
