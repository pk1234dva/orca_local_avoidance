/*
 * KdTree.cs
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

using System.Collections.Generic;
using System;

namespace Orca
{
    /// <summary>
    /// Each node has info about:
    /// 1. Sub-segment of nodes in List<agent> that it contains
    /// 2. AABB of the elements from above
    /// 3. Left and right children nodes
    /// </summary>
    internal struct AgentTreeNode
    {
        internal int begin; // index into agent array
        internal int end;
        internal int left; // index into tree nodes array (left child)
        internal int right;

        internal OrcaVector3 maxCoord;
        internal OrcaVector3 minCoord;
    }
    /// <summary>
    /// Basic KdTree class that is used for determining neighbors that are "close enough".
    ///
    /// 1. Implemented using a "agentTree" backing array that holds the various tree nodes.
    ///    Google something along the lines of "array representation of binary heap" for more info.
    ///
    /// 2. The tree is rebuilt before each simulator calcultation. Rebuilding will reorder the actual
    ///    agents in the "List<AgentBase> agents", as well as fill in the nodes in the "agentTree" array.
    ///    The tree nodes end up containing the AABB of the child nodes, as well as interval/segment
    ///    of the "agents" List that are inside that AABB.
    ///
    /// 3. Neighbor computation uses a standard Kd tree neighbor search, i.e. you recurse down, and as
    ///    you wind out, you check if the "other" segment can be ignored because it's too far away.
    ///    Google something along the lines of "kd tree Nearest neighbour search" for more info,
    ///    it shows the basic idea of what happens (somewhat different because we're essentially
    ///    seaching for say the 10 closest nearest neighbors, not just the nearest neighbor,
    ///    but the general approach is the same).
    ///
    /// </summary>
    public class KdTree
    {    
        private const int MAX_LEAF_SIZE = 10;
        //
        private List<AgentBase> agents;
        private AgentTreeNode[] agentTree;

        #region CONSTRUCTOR AND BACKING FIELDS CAPACITY UPDATE
        public KdTree(List<AgentBase> agents)
        {
            this.agents = agents;
            agentTree = new AgentTreeNode[256];
            for (int i = 0; i < agentTree.Length; ++i) agentTree[i] = new AgentTreeNode();
        }               
        internal void UpdateAgentTreeNodesCount()
        {
            // Need to make sure there's at least agents.Count * 2 many nodes available
            int agentTreeNodesThatWillBeUsed = agents.Count * 2;
            if (agentTreeNodesThatWillBeUsed > agentTree.Length)
            {
                agentTree = new AgentTreeNode[4 * agents.Count];
            }
            for (int i = 0; i < agentTreeNodesThatWillBeUsed; ++i) agentTree[i] = new AgentTreeNode();
        }
        #endregion

        #region BUILD KD-TREE
        internal void BuildAgentTree()
        {
            // Re-orders agent list elements and creates tree nodes.
            if (agents.Count != 0) BuildAgentTreeRecursive(0, agents.Count, 0);
        }       
        private void BuildAgentTreeRecursive(int begin, int end, int node)
        {
            // Set segment
            agentTree[node].begin = begin;
            agentTree[node].end = end;

            // Get AABB of the nodes from above
            agentTree[node].minCoord = agents[begin].position;
            agentTree[node].maxCoord = agents[begin].position;
            for (int i = begin + 1; i < end; ++i)
            {
                agentTree[node].maxCoord[0] = Math.Max(agentTree[node].maxCoord[0], agents[i].position.x_);
                agentTree[node].minCoord[0] = Math.Min(agentTree[node].minCoord[0], agents[i].position.x_);

                agentTree[node].maxCoord[1] = Math.Max(agentTree[node].maxCoord[1], agents[i].position.y_);
                agentTree[node].minCoord[1] = Math.Min(agentTree[node].minCoord[1], agents[i].position.y_);

                agentTree[node].maxCoord[2] = Math.Max(agentTree[node].maxCoord[2], agents[i].position.z_);
                agentTree[node].minCoord[2] = Math.Min(agentTree[node].minCoord[2], agents[i].position.z_);
            }

            // If segment too large, recursion
            if (end - begin > MAX_LEAF_SIZE)
            {
                // Determine largest axis of the AABB
                int coord;
                if (agentTree[node].maxCoord[0] - agentTree[node].minCoord[0] > agentTree[node].maxCoord[1] - agentTree[node].minCoord[1] && agentTree[node].maxCoord[0] - agentTree[node].minCoord[0] > agentTree[node].maxCoord[2] - agentTree[node].minCoord[2])
                {
                    coord = 0;
                }
                else if (agentTree[node].maxCoord[1] - agentTree[node].minCoord[1] > agentTree[node].maxCoord[2] - agentTree[node].minCoord[2])
                {
                    coord = 1;
                }
                else
                {
                    coord = 2;
                }

                // Split into halfs across the chosen axis
                float splitValue = 0.5f * (agentTree[node].maxCoord[coord] + agentTree[node].minCoord[coord]);
                
                
                // iterate through the segment
                //  L ->  <- R
                // if elements found at wrong positions, switch them
                int left = begin;
                int right = end;
                while (left < right)
                {
                    while (left < right && agents[left].position[coord] < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && agents[right - 1].position[coord] >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        AgentBase tempAgent = agents[left];
                        agents[left] = agents[right - 1];
                        agents[right - 1] = tempAgent;
                        ++left;
                        --right;
                    }
                }                

                // We may assume [begin, left] all lie in the left half
                // take the length of [begin, left]. Handle case 0
                int leftSize = left - begin;
                if (leftSize == 0)
                {
                    ++leftSize;
                    ++left;
                    ++right;
                }

                // Determine left and right node indices 
                // similar to storing a binary heap in array, we need to multiply by 2
                // we know the amount of elements of the subtree that covers [being, left] 
                // has an upper bound of [being, left] * 2
                agentTree[node].left = node + 1;
                agentTree[node].right = node + 2 * leftSize;

                // Recurse down to the nodes
                BuildAgentTreeRecursive(begin, left, agentTree[node].left);
                BuildAgentTreeRecursive(left, end, agentTree[node].right);
            }
        }
        #endregion

        #region COMPUTE NEIGHBORS
        internal void ComputeAgentNeighbors(AgentBase agent, ref float rangeSq)
        {
            QueryAgentTreeRecursive(agent, ref rangeSq, 0);
        }       
        private void QueryAgentTreeRecursive(AgentBase agent, ref float rangeSq, int node)
        {
            if (agentTree[node].end - agentTree[node].begin <= MAX_LEAF_SIZE)
            {
                for (int i = agentTree[node].begin; i < agentTree[node].end; ++i)
                {
                    agent.InsertAgentNeighbor(agents[i], ref rangeSq);
                }
            }
            else
            {
                // Calculate distance of agent from AABB of the left subtree
                float distSqLeft = 
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].left].minCoord[0] - agent.position.x())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.x() - agentTree[agentTree[node].left].maxCoord[0])) +
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].left].minCoord[1] - agent.position.y())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.y() - agentTree[agentTree[node].left].maxCoord[1])) +
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].left].minCoord[2] - agent.position.z())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.z() - agentTree[agentTree[node].left].maxCoord[2]));

                float distSqRight = 
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].right].minCoord[0] - agent.position.x())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.x() - agentTree[agentTree[node].right].maxCoord[0])) +
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].right].minCoord[1] - agent.position.y())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.y() - agentTree[agentTree[node].right].maxCoord[1])) +
                    OrcaMath.sqr(Math.Max(0.0f, agentTree[agentTree[node].right].minCoord[2] - agent.position.z())) +
                    OrcaMath.sqr(Math.Max(0.0f, agent.position.z() - agentTree[agentTree[node].right].maxCoord[2]));


                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, agentTree[node].left);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, agentTree[node].right);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, agentTree[node].right);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, agentTree[node].left);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
