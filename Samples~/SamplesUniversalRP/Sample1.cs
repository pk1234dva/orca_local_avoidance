
namespace OrcaSamples
{
    public class Sample1 : UnityEngine.MonoBehaviour
    {
        public UnityEngine.GameObject[] myPrefabs;
        public float centerOffset;
        public float stepPerAgent;
        public int sqrtAgentsPerBlock = 3;

        public void Start()
        {
            UnityEngine.Vector3 baseCenter = transform.position;

            UnityEngine.Vector3[] centerOffsets = {
            new UnityEngine.Vector3(-centerOffset, 0, -centerOffset),
            new UnityEngine.Vector3(+centerOffset, 0, -centerOffset),
            new UnityEngine.Vector3(-centerOffset, 0, +centerOffset),
            new UnityEngine.Vector3(+centerOffset, 0, +centerOffset)
            };

            UnityEngine.Vector3[] agentSteps = {
            new UnityEngine.Vector3(-stepPerAgent, 0, -stepPerAgent),
            new UnityEngine.Vector3(+stepPerAgent, 0, -stepPerAgent),
            new UnityEngine.Vector3(-stepPerAgent, 0, +stepPerAgent),
            new UnityEngine.Vector3(+stepPerAgent, 0, +stepPerAgent)
            };

            // set up each block
            for (int i = 0; i < 4; i++)
            {
                UnityEngine.Vector3 centerOffset = centerOffsets[i];
                UnityEngine.Vector3 agentStep = agentSteps[i];

                // Determine "center" of the block and "goal"
                UnityEngine.Vector3 center = baseCenter + centerOffset;
                UnityEngine.Vector3 goalOffset = -2 * centerOffset;

                // offsets per dim
                float agentStepX = agentStep.x;
                float agentStepZ = agentStep.z;

                for (int j = 0; j < sqrtAgentsPerBlock; j++)
                {
                    for (int k = 0; k < sqrtAgentsPerBlock; k++)
                    {
                        UnityEngine.Vector3 agentOffset = new UnityEngine.Vector3(j * agentStepX, 0, k * agentStepZ);
                        //
                        UnityEngine.Vector3 startPos = center + agentOffset;
                        UnityEngine.Vector3 goalPos = startPos + goalOffset;
                        //
                        var agent = Instantiate(myPrefabs[i], startPos, UnityEngine.Quaternion.identity);
                        agent.transform.SetParent(this.transform.parent);
                        var agentComponent = agent.GetComponent<Orca.AgentSample>();
                        agentComponent.TargetPosition = goalPos;
                    }
                }
            }
        }
    }
}
