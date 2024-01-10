using UnityEngine;

public class BombController : MonoBehaviour
{
    [SerializeField] private float fuseTime;
        
    private void Start()
    {
        Invoke(nameof(Explode), fuseTime);
    }

    private void Explode()
    {
        Destroy(gameObject);
    }

}
