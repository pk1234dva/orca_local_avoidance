using UnityEngine;

namespace Orca 
{
    public class SetTargetForAllChildren : MonoBehaviour
    {
        public Transform TargetTransform;
        public void Start()
        {
            AgentSample[] children = GetComponentsInChildren<AgentSample>();
            foreach (AgentSample child in children)
                child.TargetPosition = TargetTransform.position;
        }
    }
}

