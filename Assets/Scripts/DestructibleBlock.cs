using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    public void DestroyBlock()
    {
        Destroy(gameObject);
    }
}
