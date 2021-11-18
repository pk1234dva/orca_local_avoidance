
namespace Orca
{
    public class Sample2 : UnityEngine.MonoBehaviour
    {
        public UnityEngine.GameObject[] myPrefabs;
        [UnityEngine.Header("Base distance between groups")]
        public float centerOffset;
        [UnityEngine.Header("Group1 amount of elements")]
        public int group1Depth = 4;
        public int group1RowSize = 3;
        [UnityEngine.Header("Group1 distances between elements")]
        public float group1YZoffset = 5.0f;
        public float group1Xoffset = 4.0f;
        [UnityEngine.Header("Group2 amount of elements")]
        public int group2Depth = 3;
        [UnityEngine.Header("Group2 distances between elements")]
        public float group2Xoffset = 3.0f;


        public void Start()
        {
            UnityEngine.Vector3 basePosition = transform.position;

            // Create first group of spheres           
            UnityEngine.Vector3 group1Base = basePosition + new UnityEngine.Vector3(-centerOffset, 0, 0);
            // offset this further so the above is in the middle wrt YZ of the group
            float yzBaseOffset = (group1RowSize - 1) * 0.5f * group1YZoffset;
            group1Base += new UnityEngine.Vector3(0, -yzBaseOffset, -yzBaseOffset);
            for (int i = 0; i<group1Depth; i++)
            {
                for (int j = 0; j< group1RowSize; j++)
                {
                    for (int k = 0; k<group1RowSize; k++)
                    {
                        UnityEngine.Vector3 startPosition = group1Base;
                        startPosition += new UnityEngine.Vector3(group1Xoffset * i, group1YZoffset * j, group1YZoffset * k);
                        UnityEngine.Vector3 goalPosition = startPosition + new UnityEngine.Vector3(2*centerOffset, 0, 0);

                        var agent = Instantiate(myPrefabs[0], startPosition, UnityEngine.Quaternion.identity);
                        agent.transform.SetParent(this.transform.parent);
                        var agentComponent = agent.GetComponent<Orca.AgentSample>();
                        agentComponent.TargetPosition = goalPosition;
                    }
                }
            }

            // Group 2
            UnityEngine.Vector3 projectileDir = new UnityEngine.Vector3(-1, 0, 0);
            UnityEngine.Vector3 group2Base = basePosition + new UnityEngine.Vector3(+centerOffset, 0, 0);
            for (int i = 0; i< group2Depth; i++)
            {
                UnityEngine.Vector3 startPosition = group2Base;
                startPosition += new UnityEngine.Vector3(-group2Xoffset * i, 0,0);

                var projectile = Instantiate(myPrefabs[1], startPosition, UnityEngine.Quaternion.identity);
                projectile.transform.SetParent(this.transform.parent);
                var projectileComponent = projectile.GetComponent<Orca.ProjectileSample>();
                projectileComponent.SetDirection(projectileDir);
            }
        }
    }
}
