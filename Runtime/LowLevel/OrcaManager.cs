using System.Threading;
using UnityEngine;

namespace Orca {
    /// <summary>
    /// Simulator wrapper.
    /// Handles lazy initialization of the Simulator, loading settings, then regular update calls.
    /// </summary>
    public class OrcaManager : MonoBehaviour
    {
        // Inspector visible for debugging
        [SerializeField] int agentCount;

        // Private
        // Params
        // Since unity already has its own threads, it's probably a good idea to keep this relatively low
        private int maxThreadCount = 4;
        private float updateRate = 30;
        // Various
        private Simulator simulator;
        private float timer;
        private float step;

        // Static
        // Singleton related
        // For lazy initialization, keep track of whether a manager has already been initialized
        private static bool initialized = false;
        private static OrcaManager instance;
        public static OrcaManager Instance
        {
            get
            {     
                // If this is the first time this field has been referenced, create a manager
                if (instance == null && !initialized)
                {
                    // change initialized to true, to avoid any additional manager creations
                    initialized = true;

                    // create the manager
                    GameObject go = new GameObject("OrcaManager");
                    instance = go.AddComponent<OrcaManager>();

                    // try read settings, change them if found
                    var settings = Resources.Load<OrcaSettingsScriptableObject>("OrcaSettings");
                    if (settings == null)
                    {
                        Debug.Log("No orca settings found, using default settings.");
                        Debug.Log("If you want to use custom orca manager settings, add a OrcaSettingsScriptableObject asset named \"OrcaSettings\" to Assets/Plugins/OrcaManager/Resources/");
                    }
                    else
                    {
                        instance.maxThreadCount = settings.maxThreadCount;
                        instance.updateRate = settings.updateRate;
                    }

                    // initialize threads etc.
                    instance.Initialize();
                }
                return instance;
            }
        }

        #region SUB/UNSUB
        public void Subscribe(AgentBase agent)
        {
            simulator.AddAgent(agent);
        }

        public void Unsubscribe(AgentBase agent)
        {
            simulator.RemoveAgent(agent);
        }
        #endregion

        #region INIT/END
        void Initialize()
        {
            DontDestroyOnLoad(gameObject);
            simulator = new Simulator(maxThreadCount);
            //
            timer = 0.0f;

            if (updateRate > 1000.0f || updateRate < 0.001f)
            {
                Debug.LogError("Ivalid update frequency!");
                return;
            }
            step = 1.0f / updateRate;
        }
        private void OnDestroy()
        {
            simulator.Dispose();
        }
        #endregion

        #region LOOP
        public void Update()
        {
            if (timer > step)
            {
                Tick();
                timer = 0.0f;
            }
            timer += Time.unscaledDeltaTime;
        }
        #endregion

        #region TICK
        public void Tick()
        {
            // The conditional variable below is probably redundant, but I'm keeping it commented here and in the simulator
            // simulator.calculationsDone.WaitOne();

            // the "theoretical" issue is that once Monitor.Pulse below is called, we want to make sure
            // that simulator.MainWork() acquires the lock. This is not inherently guaranteed,
            // as lock acquiring is not FIFO - if we call Tick(); Tick();, it could happen that Tick() re-acquires the lock immediately!

            // It's still extremely unlikely, as Tick will not be called very frequently, and even <<~1ms should be enough delay to let MainWork() to acquire the lock.

            // Arguably, it might make sense to let Tick() run again without waiting for MainWork() in case
            // this weird scenario happens, as that would signify we're calling Tick() repeatedly, which shouldn't happen,
            // as there should be at least some delay between Tick() calls.

            lock (simulator.simulatorLock)
            {
                // Update agent list before starting next batch
                // If something has changed, list has to be updated to contain the new set of agents,
                // kd tree might potentially have to be resized, and worker indices used by threads recalculated
                simulator.UpdateAgentList();

                // Update optimized velocities, update states to reflect game position etc.
                simulator.UpdateAgentsValues();

                Monitor.Pulse(simulator.simulatorLock);
            }   

#if UNITY_EDITOR
            agentCount = simulator.GetAgentCount();
#endif
        }
        #endregion
    }
}