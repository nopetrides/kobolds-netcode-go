using UnityEngine;

namespace P3T.Scripts.Components
{
    public class Blinker : MonoBehaviour
    {
        [SerializeField] private Renderer ToBlink;
        [SerializeField] private bool InitialState;
        [SerializeField] private float BlinkRate;

        private bool _blinkState;
        private float _timer;
    
        // Start is called before the first frame
        private void Start()
        {
            _blinkState = InitialState;
            ToBlink.enabled = _blinkState;
        }

        // Update is called once per frame
        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < BlinkRate) return;
            
            _timer = 0;
            _blinkState = !_blinkState;
            ToBlink.enabled = _blinkState;
        }
    }
}
