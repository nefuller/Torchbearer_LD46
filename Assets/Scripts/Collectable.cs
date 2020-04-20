using UnityEngine;

public class Collectable : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == Layers.LAYER_PUSHABLE)
        {
            Destroy(gameObject);
        }  
    }
}
