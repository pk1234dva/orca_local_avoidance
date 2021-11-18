using UnityEngine;

namespace Orca
{
    /// <summary>
    /// Simple example of a projectile that automatically subscribes a singleton manager.
    /// </summary>
    public class ProjectileSample : AgentBase
    {
        #region IMPLEMENTATIONS, UPDATE, SUBSCRIPTIONS
        protected override UnityEngine.Vector3 UpdatePosition()
        {
            return rb.position;
        }
        protected override UnityEngine.Vector3 UpdateVelocity()
        {
            return rb.velocity;
        }
        protected override UnityEngine.Vector3 UpdateTargetVelocity()
        {            
            return default;
        }
        #endregion

        #region FIELDS
        [Header("Projectile sample parameters")]
        public float speed = 20.0f;
        public void SetDirection(UnityEngine.Vector3 direction) { rb.velocity = direction * speed; }

        // Private
        Rigidbody rb;
        #endregion

        public void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
    }
}
