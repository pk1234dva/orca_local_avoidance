/*
 * Simulator.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Orca
{
    /// <summary>
    /// Low level structure that contains the various thread initialization, calculation functions, 
    /// and data structures that keep track of the agents.
    /// </summary>
    public class Simulator
    {
        // Private fields
        private KdTree kdTree;
        private HashSet<AgentBase> agentsSet;
        private List<AgentBase> agents; // internal list, updated regularly from agentsSet

        // Thread related
        public readonly object simulatorLock;
        //public AutoResetEvent calculationsDone; // see comment in OrcaManager.Tick()
        private int numWorkers;
        private Thread[] threads;
        private Worker[] workers;
        private AutoResetEvent[] startEvents;
        private ManualResetEvent[] doneEvents;

        // Various
        private bool update = true; // keeps track of whether List<agentBase> should be updated from agentsSet
        private bool exit = false; // used for exiting main worker thread

        #region PUBLIC
        // INIT/DISPOSE
        internal Simulator(int maxThreadCount)
        {
            simulatorLock = new object();
            //calculationsDone = new AutoResetEvent(false);
            //
            agents = new List<AgentBase>(256);
            agentsSet = new HashSet<AgentBase>();
            kdTree = new KdTree(agents);
            InitializeWorkers(maxThreadCount);
        }
        internal void Dispose()
        {
            // exit main thread
            exit = true;
            lock (simulatorLock)
            {
                Monitor.Pulse(simulatorLock);
            }
        }

        // GENERAL METHODS
        /// <summary>
        /// Update optimized velocities, update states to reflect game position etc.
        /// </summary>
        internal void UpdateAgentsValues()
        {
            int agentCount = agents.Count;
            for (int i = 0; i < agentCount; i++)
            {
                AgentBase agent = agents[i];
                agent.UpdateOptimizedVelocity();
                agent.UpdateAgentState();
            }
        }

        // SUBSCRIBING AGENTS RELATED
        /// <summary>
        /// Updates internal agent list, sets kdTree reference, and updates related fields if necessary.
        /// Call this regularly to keep the internal agent list and agent set in sync.
        /// </summary>
        internal void UpdateAgentList()
        {
            if (!update) return;
            bool countChanged = UpdateAgentListAndReturnCountChanged();
            // if count changed, also update agent tree and indices
            if (countChanged)
            {
                kdTree.UpdateAgentTreeNodesCount();
                UpdateWorkerIndices();
            }
            update = false;
        }

        /// <summary>
        /// Adds agent paramater to agentsSet, sets update = true if successful.
        /// </summary>
        internal void AddAgent(AgentBase agent)
        {
            // Check if object already in dict
            if (agentsSet.Contains(agent))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError("Object already subscribed.");
#endif
                return;
            }

            // If not, add it
            agentsSet.Add(agent);
            agent.kdTree = kdTree; // note that this is never reset back to null - that's because it isn't necessary
            update = true;
        }

        /// <summary>
        /// Tries to remove agent from agentsSet, sets update = true if removed.
        /// </summary>
        internal void RemoveAgent(AgentBase agent)
        {
            // Try delete object
            bool removed = agentsSet.Remove(agent);
            if (!removed)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("Object not found in dictionary.");
#endif
                return;
            }

            // If actually removed, update
            update = true;
        }
        #endregion

        #region INTERNALS
        /// <summary>
        /// "Main" worker thread that does the KdTree update and waits for the other workers.
        /// </summary>
        internal void WorkMain()
        {
            lock (simulatorLock)
            {
                while (true)
                {
                    //calculationsDone.Set();

                    if (!exit) Monitor.Wait(simulatorLock);
                    else
                    {
                        // exit worker threads
                        for (int i = 0; i < numWorkers; i++) workers[i].Exit();
                        return;
                    }

                    // Skip if no agents
                    if (agents.Count == 0) continue;

                    // Computations
                    kdTree.BuildAgentTree();

                    // Reset the done event variables (ManualResetEvent), and signal that workers should continue
                    for (int i = 1; i < numWorkers; i++) doneEvents[i].Reset();
                    for (int i = 1; i < numWorkers; i++) startEvents[i].Set();

                    // This thread
                    workers[0].WorkFromCaller();

                    // wait for other threads to finish
                    for (int i = 1; i < doneEvents.Length; i++) doneEvents[i].WaitOne();
                }
            }
        }

        /// <summary>
        /// Update the agent list to contain the elements in the agent set.
        /// Assign kd tree to all the agents.
        /// Return if total count changed.
        /// </summary>
        /// <returns>Agents count has changed</returns>
        private bool UpdateAgentListAndReturnCountChanged()
        {
            int oldCount = agents.Count;
            //
            agents.Clear();
            var enumerator = agentsSet.GetEnumerator();
            while (enumerator.MoveNext()) agents.Add(enumerator.Current);
            //
            if (oldCount == agents.Count) return false;
            return true;
        }

        /// <summary>
        /// Creates the worker threads and the related conditional variables.
        /// </summary>
        private void InitializeWorkers(int maxThreadCount)
        {
            int hwThreadCount = UnityEngine.SystemInfo.processorCount;
            numWorkers = Math.Min(maxThreadCount, hwThreadCount);
            if (numWorkers < 1)
            {
                UnityEngine.Debug.Log("Not enough threads!, can't initialize workers.");
                return;
            }

            // Create all workers
            workers = new Worker[numWorkers];
            startEvents = new AutoResetEvent[workers.Length];
            doneEvents = new ManualResetEvent[workers.Length];

            for (int i = 0; i < numWorkers; ++i)
            {
                startEvents[i] = new AutoResetEvent(false);
                // on step, we wait for done, then reset it, so we should start with it set to true
                doneEvents[i] = new ManualResetEvent(true);
                workers[i] = new Worker(agents, 0, 0, startEvents[i], doneEvents[i]);
            }

            // Start threads for n-1 many of them
            threads = new Thread[numWorkers];
            for (int i = 1; i < numWorkers; i++) threads[i] = new Thread(workers[i].Work);
            for (int i = 1; i < numWorkers; i++) threads[i].Start();

            // Start "main" thread
            threads[0] = new Thread(WorkMain);
            threads[0].Start();
        }

        /// <summary>
        /// Updates worker indices.
        ///
        /// Each worker is responsible for computing the "optimized" velocity for a subset of all agents,
        /// if the count of agents has changed, this has to be updated.
        ///
        /// E.g. 2 workers, 20 agents, first worker updates 0-9, 2nd 10-19. If agent count changes to 25,
        /// then this has to be updated to 0-11, 12-24.
        /// </summary>
        private void UpdateWorkerIndices()
        {
            int numAgents = agents.Count;
            //
            int sizePerWorker = numAgents / workers.Length;
            int sum = 0;
            int last = numWorkers - 1;

            for (int i = 0; i < last; ++i)
            {
                int start = sum;
                sum += sizePerWorker;
                workers[i].AssignIndices(start, sum);
            }
            workers[last].AssignIndices(sum, numAgents);
        }
        #endregion

#if UNITY_EDITOR
        public int GetAgentCount()
        {
            return agents.Count;
        }
#endif
    }
    public class Worker
    {
        private List<AgentBase> agents;
        private AutoResetEvent startEvent;
        private ManualResetEvent doneEvent;
        private int start; // agent indices this worker is responsible for
        private int end;
        private bool exit;

        internal Worker(List<AgentBase> agents, int start, int end, AutoResetEvent startEvent, ManualResetEvent doneEvent)
        {
            this.agents = agents;
            this.start = start;
            this.end = end;
            this.startEvent = startEvent;
            this.doneEvent = doneEvent;
        }

        internal void Work()
        {
            while (true)
            {
                // Wait until signaled to do work
                startEvent.WaitOne();
                if (exit) return;
                for (int i = start; i < end; ++i)
                {
                    if (agents[i].agentType == AgentType.Nonsentient) continue;
                    agents[i].ComputeNeighbors();
                    agents[i].ComputeNewVelocity();
                }
                doneEvent.Set();
            }
        }

        internal void WorkFromCaller()
        {
            for (int i = start; i < end; ++i)
            {
                if (agents[i].agentType == AgentType.Nonsentient) continue;
                agents[i].ComputeNeighbors();
                agents[i].ComputeNewVelocity();
            }
        }

        public void Exit()
        {
            exit = true;
            startEvent.Set();
        }

        public void AssignIndices(int start, int end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
