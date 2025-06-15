using System;
using System.Collections;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public enum ScreenArea
    {
        SafeArea,
        CompleteScreen,
        Notch,
    }

    [HelpURL("https://documentation.therabytes.de/better-ui/ResizeToScreenArea.html")]
    public class ResizeToScreenArea : MonoBehaviour, IResolutionDependency
    {
        [SerializeField] ScreenArea screenArea = ScreenArea.SafeArea;
        [SerializeField] bool keepAnchoredPositionAndSizeDelta = false;

        Canvas canvas;

        void OnEnable()
        {
            ResizeToArea();
        }

        public void OnResolutionChanged()
        {
            // The changing of resolution usually needs a frame before everything is set up (like updated safe area)
            // Therefore, the resize should happen in the next frame.
            ResizeToAreaDelayed();
        }
        
        void ResizeToAreaDelayed()
        {
            StopAllCoroutines();
            StartCoroutine(ResizeToAreaDelayedCoroutine());
        }

        IEnumerator ResizeToAreaDelayedCoroutine()
        {
            yield return null;
            ResizeToArea();
        }

        void ResizeToArea()
        {
            var parent = this.transform.parent as RectTransform;
            if (parent == null)
            {
                Debug.LogError("Cannot apply screen area: parent is not a rect transform.");
                return;
            }

            Vector2 res = ResolutionMonitor.CurrentResolution;
            Rect areaScreenRect;
            switch (screenArea)
            {
                case ScreenArea.SafeArea:
                    areaScreenRect = Flip(Screen.safeArea);
                    break;
                case ScreenArea.CompleteScreen:
                    areaScreenRect = new Rect(0, 0, res.x, res.y);
                    break;
                case ScreenArea.Notch:
                    if (Screen.cutouts.Length == 0)
                        return;

                    areaScreenRect = Flip(Screen.cutouts[0]);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var cam = GetCamera();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, areaScreenRect.min, cam, out var min))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, areaScreenRect.max, cam, out var max))
                {
                    var rt = transform as RectTransform;
                    rt.anchorMin = min;
                    rt.anchorMax = max;

                    Vector2 size = parent.rect.size;

                    if (size.x == 0 || size.y == 0) // prevent division by zero
                        return;

                    Vector2 pp = parent.pivot;
                    min = new Vector2(pp.x + min.x / size.x, pp.y + min.y / size.y);
                    max = new Vector2(pp.x + max.x / size.x, pp.y + max.y / size.y);

                    rt.anchorMin = min;
                    rt.anchorMax = max;

                    if (!keepAnchoredPositionAndSizeDelta)
                    {
                        rt.anchoredPosition = Vector2.zero;
                        rt.sizeDelta = Vector2.zero;
                    }
                }
            }
        }

        private Rect Flip(Rect area)
        {
            Vector2 res = ResolutionMonitor.CurrentResolution;
            return new Rect(area.x, res.y - area.height - area.y, area.width, area.height);
        }

        private Camera GetCamera()
        {
            Camera cam = null;
            if (canvas == null)
            {
                canvas = this.transform.GetComponentInParent<Canvas>();
            }

            if (canvas != null)
            {
                cam = canvas.worldCamera;
            }

            return cam;
        }
    }
}
