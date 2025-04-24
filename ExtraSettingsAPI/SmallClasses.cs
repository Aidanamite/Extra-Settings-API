using UnityEngine;
using UnityEngine.UI;
using System;

public class ToggleImage : MonoBehaviour
{
    Toggle obj;
    bool last;
    public Sprite on;
    public Sprite off;
    void Awake()
    {
        obj = GetComponent<Toggle>();
        last = !obj.isOn;
    }
    void Update()
    {
        if (last != obj.isOn && obj.image)
        {
            last = obj.isOn;
            obj.image.sprite = last ? on : off;
        }
    }
}

public class WaitForFirstUpdate : MonoBehaviour
{
    public Action onFirstUpdate;
    void Update()
    {
        onFirstUpdate?.Invoke();
        DestroyImmediate(this);
    }
}
