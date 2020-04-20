using UnityEngine;

public class Key : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == Layers.LAYER_PUSHABLE)
        {
            Destroy(gameObject);
        }
    }
}
