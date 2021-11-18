using UnityEngine;

namespace Orca
{
    /// <summary>
    /// Simple example of an agent that references a singleton manager.
    /// </summary>
    public class AgentSample : AgentBase
    {
        #region IMPLEMENTATIONS        
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
            float cutoffDist = 5.0f;
            UnityEngine.Vector3 diff = TargetPosition - rb.position;
            float diffMagnitude = diff.magnitude;
           
            // Set speed, lower if close to goal
            UnityEngine.Vector3 result = diff.normalized * preferredSpeed;
            if (diffMagnitude < cutoffDist) 
            {
                float ratio = diffMagnitude / cutoffDist;
                result *= ratio * ratio;
            } 
            return result;            
        }
        #endregion

        #region UPDATE
        public void FixedUpdate()
        {
            if (!ModifyVelocityDirectly)
            {
                UnityEngine.Vector3 diff = OptimizedVelocity - rb.velocity;
                UnityEngine.Vector3 accel = UnityEngine.Vector3.ClampMagnitude(diff, maxAccel);
                rb.AddForce(accel, ForceMode.Acceleration);
            }
            else
            {
                rb.velocity = OptimizedVelocity;
            }
        }
        #endregion

        #region FIELDS
        [Header("Agent sample parameters")]
        [SerializeField] float preferredSpeed = 10.0f;
        [SerializeField] float maxAccel = 50;
        public UnityEngine.Vector3 TargetPosition;
        [SerializeField] bool ModifyVelocityDirectly = false;

        // Private
        Rigidbody rb;
        #endregion

        public void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
    }
}
