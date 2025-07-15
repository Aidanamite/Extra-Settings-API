using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace _ExtraSettingsAPI
{
    public class ToggleImage : MonoBehaviour
    {
        Toggle obj;
        bool last;
        public Sprite on;
        public Sprite off;
        void Awake()
        {
            obj = GetComponent<Toggle>();
        }
        void Start()
        {
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
            try
            {
                onFirstUpdate?.Invoke();
            }
            finally
            {
                DestroyImmediate(this);
            }
        }
    }

    public class SettingHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ModSetting owner;
        public void OnPointerEnter(PointerEventData data) => ExtraSettingsAPI.OnSettingHover(owner, owner.GetTooltip);
        public void OnPointerExit(PointerEventData data) => ExtraSettingsAPI.OnSettingHoverStop(owner);
    }

    public class BasicTooltipHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Message;
        public void OnPointerEnter(PointerEventData data) => ExtraSettingsAPI.OnSettingHover(this, Message);
        public void OnPointerExit(PointerEventData data) => ExtraSettingsAPI.OnSettingHoverStop(this);
    }

    [RequireComponent(typeof(RectTransform))]
    public class TooltipController : MonoBehaviour
    {
        RectTransform rect;
        public Text text;
        public void Awake() => rect = GetComponent<RectTransform>();
        public void LateUpdate()
        {
            var zero = (Vector2)rect.InverseTransformPoint(Vector3.zero);
            var pos = (Vector2)rect.InverseTransformPoint(Input.mousePosition) - zero;
            var parentSize = (Vector2)rect.InverseTransformPoint(new Vector3(Screen.width,Screen.height)) - zero;
            var size = text.GetPreferredSize(Math.Min(text.fontSize * 30, parentSize.x - text.fontSize / 2));
            size.x += text.fontSize;
            size.y += text.fontSize;
            if (pos.x + size.x > parentSize.x)
                pos.x = parentSize.x - size.x;
            if (pos.y + size.y > parentSize.y && pos.y - size.y >= 0)
            {
                pos.y -= size.y;
                pos += (Vector2)rect.InverseTransformPoint(new Vector3(0, -32, 0)) - zero;
            }
            rect.offsetMax = pos + size;
            rect.offsetMin = pos;
        }
    }

    public class PushInButton : MonoBehaviour
    {
        public float padding;
        [SerializeField]
        List<RectTransform> pushed = new List<RectTransform>();
        public void OnEnable()
        {
            var rect = GetComponent<RectTransform>();
            var size = rect.offsetMax.x - rect.offsetMin.x + padding;
            foreach (RectTransform child in transform.parent)
                if (child != transform && child.anchorMin.x == 0 && rect.offsetMin.x - padding <= child.offsetMin.x && !pushed.Contains(child))
                {
                    pushed.Add(child);
                    child.offsetMin += new Vector2(size, 0);
                    if (child.anchorMax.x == 0)
                        child.offsetMax += new Vector2(size, 0);
                }
        }
        public void OnDisable()
        {
            var rect = GetComponent<RectTransform>();
            var size = rect.offsetMax.x - rect.offsetMin.x + padding;
            foreach (var child in pushed)
            {
                child.offsetMin -= new Vector2(size, 0);
                if (child.anchorMax.x == 0)
                    child.offsetMax -= new Vector2(size, 0);
            }
            pushed.Clear();
        }
    }

    public enum Rounding
    {
        Lowest,
        Nearest,
        Highest,
        Floor = Lowest,
        Round = Nearest,
        Cieling = Highest
    }
}