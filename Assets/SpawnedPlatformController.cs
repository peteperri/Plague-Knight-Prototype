using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedPlatformController : MonoBehaviour
{
    [SerializeField] private Color targetColor = new Color(255, 0, 0, 0);
    [SerializeField] private float lifeTimeSeconds = 5;
    
    private Material _materialToChange;
    void Start()
    {
        _materialToChange = gameObject.GetComponent<Renderer>().material;
        StartCoroutine(LerpFunction(targetColor, lifeTimeSeconds));
    }
    
    IEnumerator LerpFunction(Color endValue, float duration)
    {
        float time = 0;
        Color startValue = _materialToChange.color;
        while (time < duration)
        {
            _materialToChange.color = Color.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        _materialToChange.color = endValue;
        Destroy(gameObject);
    }
    
}
