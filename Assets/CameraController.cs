using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject player;

    void Start()
    {
        player = FindObjectOfType<PlagueController>().gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 playerPos = player.transform.position;
        transform.position = new Vector3(playerPos.x, transform.position.y, transform.position.z);
    }
}
