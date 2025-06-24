using UnityEngine;

namespace FIMSpace.Basics
{
    /// <summary>
    /// Basic script to hold initial position of object
    /// </summary>
    public class FBasic_HoldPosition : MonoBehaviour
    {
        [Tooltip("Multiplies deltaTime")]
        public float HoldPower = 60f;

        public bool ResetRigidbodyVelocity = false;

        protected Vector3 initialPosition;
        protected Rigidbody rigidbodyToHold;

        protected virtual void Start()
        {
            initialPosition = transform.position;
            if (ResetRigidbodyVelocity) rigidbodyToHold = GetComponent<Rigidbody>();
        }

        protected virtual void Update()
        {
            if ( rigidbodyToHold )  rigidbodyToHold.linearVelocity = Vector3.Lerp(rigidbodyToHold.linearVelocity, Vector3.zero, Time.deltaTime * HoldPower);

            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * HoldPower);
        }
    }

}
