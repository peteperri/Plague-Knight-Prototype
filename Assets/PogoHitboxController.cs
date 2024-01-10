using UnityEngine;

public class PogoHitboxController : MonoBehaviour
{
    public bool OverlappingPogoable { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pogo"))
        {
            //Debug.Log("OVERLAPPING POGO");
            OverlappingPogoable = true;
        }
        else
        {
            OverlappingPogoable = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Pogo"))
        {
            OverlappingPogoable = false;
        }
    }
}
