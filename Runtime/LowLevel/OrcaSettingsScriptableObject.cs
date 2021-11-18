using UnityEngine;

namespace Orca
{
    /// <summary>
    /// Holds various settings concerning the orca manager.
    /// Set during initialization - cannot be modified at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "OrcaSettings", menuName = "ScriptableObjects/OrcaSettings", order = 1)]
    public class OrcaSettingsScriptableObject : ScriptableObject
    {
        public int maxThreadCount = 4;
        public float updateRate = 30;
    }
}