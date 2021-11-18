/*
 * Agent.cs
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
using static Orca.OrcaMath;

namespace Orca
{
    public enum AgentType
    {
        Sentient,
        Nonsentient,
    }
    public enum WeightType
    {
        Fixed,
        // This agent is responsible for a fixed % of dodging, ignoring what the other agent is/does
        LinearInterpolation,
        // if this weight = 1, other weight = 10 -> this will dodge 10x more
    }

    public enum DodgeType
    {
        TreatProjectileAsIfAgent,
        None,
        Fixed,
        Standard,
    }

    public abstract class AgentBase : UnityEngine.MonoBehaviour
    {
        // ---------------------//
        // -- USER API START -- //
        // ---------------------//

        // use this to get results
        [UnityEngine.HideInInspector] public UnityEngine.Vector3 OptimizedVelocity { get; private set; }

        // implement these in your custom AgentBase extensions
        protected abstract UnityEngine.Vector3 UpdatePosition();
        protected abstract UnityEngine.Vector3 UpdateVelocity();
        protected abstract UnityEngine.Vector3 UpdateTargetVelocity();

        // ---------------------//
        // --- USER API END --- //
        // ---------------------//

        #region FIELDS
        // Serialized fields

        // Agent type
        [UnityEngine.Header("Avoidance parameters")]
        [UnityEngine.Tooltip("Sentient are standard agents that avoid other agents. Non-sentient can be used for projectiles etc. - this object should be avoided by others, but it doesn't avoid anything itself.")]
        [UnityEngine.SerializeField] private AgentType _agentType = AgentType.Sentient;
        public AgentType agentType { get { return _agentType; } }

        // Weight type
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Method for calculating the amount of responsibility this agent takes for avoiding other sentient agents.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient)]
        private WeightType sentientAvoidanceType = WeightType.LinearInterpolation;
        public WeightType weightType { get { return sentientAvoidanceType; } }

        // Weight
        [UnityEngine.SerializeField]

        [UnityEngine.Tooltip("Used for determining dodging value against sentient agents.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 100000.0f)]
        private float dodgeSentientWeight = 100.0f;
        public float weight { get { return dodgeSentientWeight; } }

        // Projectile weight
        [UnityEngine.SerializeField]

        [UnityEngine.Tooltip("How much should this projectile be avoided? Used by sentient agents as a factor to determining how much this projectile should be avoided.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Nonsentient, 100000.0f)]
        private float _projectileWeight = 100.0f;
        public float projectileWeight { get { return _projectileWeight; } }


        // Dodge type
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Method for calculating the amount of avoidance against projectiles.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient)]
        private DodgeType projectileAvoidanceType = DodgeType.Standard;

        // Dodge weight [0, 1] - can use default "max" parameter for DrawRangeIfEnum attribute
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Used for determining dodging value against non-sentient agents.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient)]
        private float dodgeNonsentientWeight = 0.01f;


        // Base parameters
        [UnityEngine.Header("Agent base parameters")]
        [UnityEngine.Tooltip("Radius of the agent object. Use a larger value than the real radius if using acceleration based agent modification.")]
        [UnityEngine.SerializeField]
        private float radius = 2.0f;

        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 150.0f)]
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Maximum possible speed of this agent considered to be valid by the simulator. Optimized velocity magnitude will be less or equal to this.")]
        protected float maxSpeed = 10.0f; // works as a clamp on what the linear programs consider to be valid results

        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Maximum amount of sentient agents that this agent will try to avoid. The _this_value_ closest agents will be used for avoidance calculations. ")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 200.0f)]
        private int maxNeighbors = 15;

        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Sentient agents maximum range. Agents beyond this range will be ignored.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 100.0f)]
        private float neighborDist = 15.0f;

        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Projectile/non-sentient agents maximum range. Agents beyond this range will be ignored.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 300.0f)]
        private float projectileDist = 15.0f;

        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("Expected collisions within this time will be avoided. If set too high, agents will start avoiding each other even when very far away, when it can be possible that their pathfinding would eventually lead them different paths, with no local avoidance necessary.")]
        [DrawRangeIfEnum("_agentType", (int)AgentType.Sentient, 10.0f)]
        private float timeHorizon = 2.0f;

        // Private fields
        private OrcaManager orcaManager { get { return OrcaManager.Instance; } }
        // the four following fields are bit redundant, only really need one of these in each worker... but I don't feel like changing this
        private List<KeyValuePair<float, AgentBase>> agentNeighbors = new List<KeyValuePair<float, AgentBase>>(16);
        private List<KeyValuePair<float, AgentBase>> agentNeighborsProjectiles = new List<KeyValuePair<float, AgentBase>>(16);
        private List<Plane> orcaPlanes = new List<Plane>(16);
        private List<Plane> projPlanes = new List<Plane>(16);
        // Agent needs access to the kd tree for neighbor calculations
        public KdTree kdTree { set; private get; }
        //
        [UnityEngine.HideInInspector] internal OrcaVector3 position;
        protected OrcaVector3 prefVelocity; // ideal velocity - linear programs try to get as close as possible to this value
        protected OrcaVector3 velocity; // current velocity - current velocity of this and the "other" agent are used to determine the velocity obstacle
        // stores the computation and its result
        // however, to avoid race conditions, do not access this directly
        protected OrcaVector3 _velocityComputationVariable;   
        // property to allow easy debugging
        protected OrcaVector3 velocityComputationVariable
        {
            get
            {
                return _velocityComputationVariable;
            }

            set
            {
                _velocityComputationVariable = value;
            }
        }
        //
        private const float invTimeStepCollision = 1.0f / 0.25f;
        private bool addToNonsentient;
        #endregion

        #region UPDATE
        internal void UpdateOptimizedVelocity() 
        {
            if (OrcaMath.ValidVector3(velocityComputationVariable)) 
                OptimizedVelocity = velocityComputationVariable;
            else
            {
                UnityEngine.Debug.LogWarning(ErrorOptimizedVelocityInvalid);
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(gameObject);
                UnityEngine.Debug.LogWarning(velocityComputationVariable);
#endif
            }
        }
        private const string ErrorOptimizedVelocityInvalid =
            "Optimized velocity calculation is infinite or NaN. This is likely a result of the situation being unsolvable and lack of numerical precision. Consider raising parameters like TimeHorizon or NeighborDist";

        internal void UpdateAgentState()
        {
            position = Unity2Rvo(UpdatePosition());
            velocity = Unity2Rvo(UpdateVelocity());
            prefVelocity = Unity2Rvo(Pertube(UpdateTargetVelocity()));
        }

        const float eps = 0.00001f;
        private UnityEngine.Vector3 Pertube(UnityEngine.Vector3 vector)
        {
            return vector + eps * UnityEngine.Random.insideUnitSphere;
        }
        #endregion

        #region SUB / UNSUB
        protected void Subscribe()
        {
            orcaManager.Subscribe(this);
        }
        protected void Unsubscribe()
        {
            // if orcaManager has already been disposed, then we don't need to do anything
            if (orcaManager != null)
                orcaManager.Unsubscribe(this);
        }

        public void OnEnable()
        {
            Subscribe();
        }
        public void OnDisable()
        {
            Unsubscribe();
        }
        #endregion

        #region COMPUTATION
        internal void ComputeNeighbors()
        {
            if (kdTree == null)
                UnityEngine.Debug.LogError("No kd tree assigned! Make sure you are not updating agents while computation has not finished - call simulator.WaitForWorkers() before simulator.UpdateAgentList() is called.");

            agentNeighbors.Clear();
            agentNeighborsProjectiles.Clear();
            float rangeSq;

            // This is pretty messy and inefficient, adding another rangeSq float that would be used for projectiles
            // would be better
            if (maxNeighbors > 0)
            {
                // Sentient neighbors
                addToNonsentient = false; // set this field to make sure inserting goes into sentient
                rangeSq = OrcaMath.sqr(neighborDist);
                kdTree.ComputeAgentNeighbors(this, ref rangeSq);

                // Nonsentient neighbors
                addToNonsentient = true;
                rangeSq = OrcaMath.sqr(projectileDist);
                kdTree.ComputeAgentNeighbors(this, ref rangeSq);
            }
        }

        internal void InsertAgentNeighbor(AgentBase agent, ref float rangeSq)
        {
            List<KeyValuePair<float, AgentBase>> agentNeighborsCurrent;

            // If currently processing sentient agents
            if (addToNonsentient == false)
            {
                // skip if not sentient
                if (!(agent.agentType == AgentType.Sentient)) return;
                agentNeighborsCurrent = agentNeighbors;
            }
            // If currently processing nonsentient agents
            else
            {
                // skip if not nonsentient
                if (!(agent.agentType == AgentType.Nonsentient)) return;
                agentNeighborsCurrent = agentNeighborsProjectiles;
            }

            // Avoid adding itself
            if (this != agent)
            {
                float distSq = OrcaMath.absSq(position - agent.position);

                if (distSq < rangeSq)
                {
                    // if enough space, first add new agent to end of list
                    if (agentNeighborsCurrent.Count < maxNeighbors)
                    {
                        agentNeighborsCurrent.Add(new KeyValuePair<float, AgentBase>(distSq, agent));
                    }

                    int i = agentNeighborsCurrent.Count - 1;

                    // we want to keep the neighbors ordered by distance from this.position,
                    // so we start from the end, and move all neighbors larger than current new to the right
                    while (i != 0 && distSq < agentNeighborsCurrent[i - 1].Key)
                    {
                        agentNeighborsCurrent[i] = agentNeighborsCurrent[i - 1];
                        --i;
                    }

                    agentNeighborsCurrent[i] = new KeyValuePair<float, AgentBase>(distSq, agent);

                    // if list is full, last element gives a new upper bound on radius
                    if (agentNeighborsCurrent.Count == maxNeighbors)
                    {
                        rangeSq = agentNeighborsCurrent[agentNeighborsCurrent.Count - 1].Key;
                    }
                }
            }
        }
        internal void ComputeNewVelocity()
        {
            orcaPlanes.Clear();
            float invTimeHorizon = 1.0f / timeHorizon;

            // Determine total count by combining sentient and nonsentient agents
            int agentNeighborsCount = agentNeighbors.Count;
            int totalCount = agentNeighborsCount + agentNeighborsProjectiles.Count;

            /* Create agent ORCA planes. */
            // iterate through both agentNeighbors and agentNeighborsProjectiles
            for (int i = 0; i < totalCount; ++i)
            {
                AgentBase other;
                if (i < agentNeighborsCount) other = agentNeighbors[i].Value;
                else other = agentNeighborsProjectiles[i - agentNeighborsCount].Value;

                // AgentBase other = agentNeighbors[i].Value;
                OrcaVector3 relativePosition = other.position - position;
                OrcaVector3 relativeVelocity = velocity - other.velocity;
                float distSq = OrcaMath.absSq(relativePosition);
                float combinedRadius = radius + other.radius;
                float combinedRadiusSq = OrcaMath.sqr(combinedRadius);

                Plane plane;
                OrcaVector3 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    OrcaVector3 w = relativeVelocity - invTimeHorizon * relativePosition;

                    // note: w*delta ~= distance from center of other after delta.

                    // invTimeHorizon * relativePosition corresponds to the velocity
                    // such that after time horizon, agent will be at other's center

                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = OrcaMath.absSq(w);

                    float dotProduct = w * relativePosition;

                    // dot product < 0 gives an upper bound - if >= 0, projection is not on the cutoff circle
                    if (dotProduct < 0.0f && OrcaMath.sqr(dotProduct) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = OrcaMath.sqrt(wLengthSq);
                        OrcaVector3 unitW = w / wLength;

                        plane.normal = unitW;
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on cone. */

                        #region ORIGINAL

                        // Approach used by original RVO authors:

                        // 1,
                        // c = t * relativePosition is the point on the cone axis, such that
                        // the sphere with origin c ( of radius "combinedRadius * t"), is tangent to the projection of
                        // the relative velocity onto the cone, and "ww = relativeVelocity - c"
                        // -> thus to get the actual u vector, we use "u = (combinedRadius * t - wwLength) * unitWW;"

                        float a = distSq;
                        float b = relativePosition * relativeVelocity;

                        // no idea what the second term is supposed to be...
                        // also don't understand what the distSq - combinedRadiusSq is supposed to do and why we divide by it
                        float c = OrcaMath.absSq(relativeVelocity) - OrcaMath.absSq(OrcaVector3.cross(relativePosition, relativeVelocity)) / (distSq - combinedRadiusSq);

                        // if a=1 -> b is clear, but I don't see how RVOMath.sqrt(RVOMath.sqr(b) - a * c) makes sense
                        // it should be "what remains" to get to the "c" described above, but I don't see how it is
                        float t = (b + OrcaMath.sqrt(OrcaMath.sqr(b) - a * c)) / a;


                        OrcaVector3 ww = relativeVelocity - t * relativePosition;
                        float wwLength = OrcaMath.abs(ww);
                        OrcaVector3 unitWW = ww / wwLength;

                        plane.normal = unitWW;
                        u = (combinedRadius * t - wwLength) * unitWW;

                        #endregion

                        #region MY OWN ALTERNATIVE
                        // note: one easy way of optimizing this would be to make sure inverse square root is used all over the place
                        // not sure why the original authors took such effort to write this projection code in the way they did,
                        // and then did normalization via sqrt and then division?

                        // Direction from this agent to other agent
                        /*OrcaVector3 relPosNormalized = OrcaMath.normalize(relativePosition);

                        // Direction from cone axis to relative velocity
                        OrcaVector3 relVelProjOnAxis = relPosNormalized * (relPosNormalized * relativeVelocity);
                        OrcaVector3 axis2RelVel = relativeVelocity - relVelProjOnAxis;
                        OrcaVector3 axis2RelVelNormalized = OrcaMath.normalize(axis2RelVel);

                        // Calculate the ratio between the shorter sides of the triangle 
                        // [ Origin ] [sphere center] [tangent point on cone]
                        float cSquared = distSq;
                        float bSquared = combinedRadiusSq;
                        float aSquared = cSquared - bSquared;
                        float ratio = combinedRadius / OrcaMath.sqrt(aSquared);

                        // Normal of the tangent plane of the cone, on which we can project the relative velocity
                        OrcaVector3 tangentPlaneVector = axis2RelVelNormalized - ratio * relPosNormalized;
                        OrcaVector3 normal = OrcaMath.normalize(tangentPlaneVector); 
                        // the direction is "outside" - from cone axis to the cone

                        // Project rel vel
                        float relVelProjOnNormal = relativeVelocity * normal;

                        // Assign the values
                        plane.normal = normal;
                        u = -normal * relVelProjOnNormal;*/
                        #endregion

                    }
                }
                else
                {
                    // if in a collision, we want to choose such a velocity,
                    // that after a single time-step, we'll be out of this collision
                    
                    // MODIFICATION: might not be necessary to use a time step value - just use a fixed amount instead. I've opted to do that here.

                    /* Collision*/
                    float invTimeStep = invTimeStepCollision;
                    OrcaVector3 w = relativeVelocity - invTimeStep * relativePosition;
                    float wLength = OrcaMath.abs(w);
                    OrcaVector3 unitW = w / wLength;

                    plane.normal = unitW;
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                plane.point = velocity + u * CalculateRelativeWeight(other);
                orcaPlanes.Add(plane);
            }

            int planeFail = linearProgram3(orcaPlanes, maxSpeed, prefVelocity, false);

            if (planeFail < orcaPlanes.Count)
            {
                linearProgram4(orcaPlanes, planeFail, maxSpeed);
            }
        }
        private float CalculateRelativeWeight(AgentBase other)
        {
            // If other agent is sentient,
            // or if we treat non-sentient agents the same way as sentient
            if (other.agentType == AgentType.Sentient || projectileAvoidanceType == DodgeType.TreatProjectileAsIfAgent)
            {
                if (weightType == WeightType.Fixed)
                    return UnityEngine.Mathf.Clamp01(weight);
                if (weightType == WeightType.LinearInterpolation)
                    return other.weight / (weight + other.weight + 0.0001f);
            }

            // if other agent is for example a projectile, how much do we dodge?
            if (other.agentType == AgentType.Nonsentient)
            {
                if (projectileAvoidanceType == DodgeType.None)
                    return 0.0f;
                if (projectileAvoidanceType == DodgeType.Fixed)
                    return UnityEngine.Mathf.Clamp01(dodgeNonsentientWeight);
                if (projectileAvoidanceType == DodgeType.Standard)
                {
                    // Example:
                    // projectileWeight for projectiles should by default be 100.
                    // dodgeWeight default is 0.01
                    // result is 1 -> this agent "feels" fully responsible for dodging
                    // however if the weight of the projectile is 50, it will not dodge fully immediately
                    return UnityEngine.Mathf.Clamp01(dodgeNonsentientWeight * other.projectileWeight);
                }
            }

            return 0.5f;
        }
        #endregion

        #region LINEAR PROGRAMS

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool linearProgram1(List<Plane> planes, int planeNo, ref Line line, float radius, OrcaVector3 optVelocity, bool directionOpt)
        {
            float dotProduct = dotProduct = line.point * line.direction;
            float discriminant = OrcaMath.sqr(dotProduct) + OrcaMath.sqr(radius) - OrcaMath.absSq(line.point);

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }
                        
            float sqrtDiscriminant = OrcaMath.sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < planeNo; ++i)
            {      
                float numerator = (planes[i].point - line.point) * planes[i].normal;
                float denominator = line.direction * planes[i].normal;

                if (OrcaMath.sqr(denominator) <= OrcaMath.RVO_EPSILON)
                {
                    /* Lines line is (almost) parallel to plane i. */
                    if (numerator > 0.0f)
                    {
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Plane i bounds line on the left. */
                    tLeft = Math.Max(tLeft, t);
                }
                else
                {
                    /* Plane i bounds line on the right. */
                    tRight = Math.Min(tRight, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (optVelocity * line.direction > 0.0f)
                {
                    /* Take right extreme. */
                    velocityComputationVariable = line.point + tRight * line.direction;
                }
                else
                {
                    /* Take left extreme. */
                    velocityComputationVariable = line.point + tLeft * line.direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = line.direction * (optVelocity - line.point);

                if (t < tLeft)
                {
                    velocityComputationVariable = line.point + tLeft * line.direction;
                }
                else if (t > tRight)
                {
                    velocityComputationVariable = line.point + tRight * line.direction;
                }
                else
                {
                    velocityComputationVariable = line.point + t * line.direction;
                }
            }

            return true;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */	
        private bool linearProgram2(List<Plane> planes, int planeNo, float radius, OrcaVector3 optVelocity, bool directionOpt)
        {
            float planeDist = planes[planeNo].point * planes[planeNo].normal;
            float planeDistSq = OrcaMath.sqr(planeDist);
            float radiusSq = OrcaMath.sqr(radius);

            if (planeDistSq > radiusSq)
            {
                /* Max speed sphere fully invalidates plane planeNo. */
                return false;
            }

            float planeRadiusSq = radiusSq - planeDistSq;

            OrcaVector3 planeCenter = planeDist * planes[planeNo].normal;

            if (directionOpt)
            {
                /* Project direction optVelocity on plane planeNo. */
                OrcaVector3 planeOptVelocity = optVelocity - (optVelocity * planes[planeNo].normal) * planes[planeNo].normal;
                float planeOptVelocityLengthSq = OrcaMath.absSq(planeOptVelocity);

                if (planeOptVelocityLengthSq <= OrcaMath.RVO_EPSILON)
                {
                    velocityComputationVariable = planeCenter;
                }
                else
                {
                    velocityComputationVariable = planeCenter + OrcaMath.sqrt(planeRadiusSq / planeOptVelocityLengthSq) * planeOptVelocity;
                }
            }
            else
            {
                /* Project point optVelocity on plane planeNo. */
                velocityComputationVariable = optVelocity + ((planes[planeNo].point - optVelocity) * planes[planeNo].normal) * planes[planeNo].normal;

                /* If outside planeCircle, project on planeCircle. */
                if (OrcaMath.absSq(velocityComputationVariable) > radiusSq)
                {
                    OrcaVector3 planeResult = velocityComputationVariable - planeCenter;
                    float planeResultLengthSq = OrcaMath.absSq(planeResult);
                    velocityComputationVariable = planeCenter + OrcaMath.sqrt(planeRadiusSq / planeResultLengthSq) * planeResult;
                }
            }

            for (int i = 0; i < planeNo; ++i)
            {
                if (planes[i].normal * (planes[i].point - velocityComputationVariable) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    /* Compute intersection line of plane i and plane planeNo. */
                    OrcaVector3 crossProduct = OrcaVector3.cross(planes[i].normal, planes[planeNo].normal);

                    if (OrcaMath.absSq(crossProduct) <= OrcaMath.RVO_EPSILON)
                    {
                        /* Planes planeNo and i are (almost) parallel, and plane i fully invalidates plane planeNo. */
                        return false;
                    }

                    Line line;
                    line.direction = OrcaMath.normalize(crossProduct);
                    OrcaVector3 lineNormal = OrcaVector3.cross(line.direction, planes[planeNo].normal);
                    line.point = planes[planeNo].point + (((planes[i].point - planes[planeNo].point) * planes[i].normal) / (lineNormal * planes[i].normal)) * lineNormal;

                    if (!linearProgram1(planes, i, ref line, radius, optVelocity, directionOpt))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        int linearProgram3(List<Plane> planes, float radius, OrcaVector3 optVelocity, bool directionOpt)
        {
            if (directionOpt)
            {
                /* Optimize direction. Note that the optimization velocity is of unit length in this case. */
                velocityComputationVariable = optVelocity * radius;
            }
            else if (OrcaMath.absSq(optVelocity) > OrcaMath.sqr(radius))
            {
                /* Optimize closest point and outside circle. */
                velocityComputationVariable = OrcaMath.normalize(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                velocityComputationVariable = optVelocity;
            }

            for (int i = 0; i < planes.Count; ++i)
            {
                if (planes[i].normal * (planes[i].point - velocityComputationVariable) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    OrcaVector3 tempResult = velocityComputationVariable;

                    if (!linearProgram2(planes, i, radius, optVelocity, directionOpt))
                    {
                        velocityComputationVariable = tempResult;
                        return i;
                    }
                }
            }

            return planes.Count;
        }

        void linearProgram4(List<Plane> planes, int beginPlane, float radius)
        {
            float distance = 0.0f;

            for (int i = beginPlane; i < planes.Count; ++i)
            {
                if (planes[i].normal * (planes[i].point - velocityComputationVariable) > distance)
                {
                    /* Result does not satisfy constraint of plane i. */
                    //List<Plane> projPlanes_ = new List<Plane>();
                    projPlanes.Clear();

                    for (int j = 0; j < i; ++j)
                    {
                        Plane plane;

                        OrcaVector3 crossProduct = OrcaVector3.cross(planes[j].normal, planes[i].normal);

                        if (OrcaMath.absSq(crossProduct) <= OrcaMath.RVO_EPSILON)
                        {
                            /* Plane i and plane j are (almost) parallel. */
                            if (planes[i].normal * planes[j].normal > 0.0f)
                            {
                                /* Plane i and plane j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Plane i and plane j point in opposite direction. */
                                plane.point = 0.5f * (planes[i].point + planes[j].point);
                            }
                        }
                        else
                        {
                            /* Plane.point is point on line of intersection between plane i and plane j. */
                            OrcaVector3 lineNormal = OrcaVector3.cross(crossProduct, planes[i].normal);
                            plane.point = planes[i].point + (((planes[j].point - planes[i].point) * planes[j].normal) / (lineNormal * planes[j].normal)) * lineNormal;
                        }

                        plane.normal = OrcaMath.normalize(planes[j].normal - planes[i].normal);
                        projPlanes.Add(plane);
                    }

                    OrcaVector3 tempResult = velocityComputationVariable;

                    if (linearProgram3(projPlanes, radius, planes[i].normal, true) < projPlanes.Count)
                    {
                        /* This should in principle not happen.  The result is by definition already in the feasible region of this linear program. If it fails, it is due to small floating point error, and the current result is kept. */
                        velocityComputationVariable = tempResult;
                    }

                    distance = planes[i].normal * (planes[i].point - velocityComputationVariable);
                }
            }
        }
        #endregion

        #region EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEngine.Gizmos.color = UnityEngine.Color.red;
            UnityEngine.Gizmos.DrawWireSphere(transform.position, radius * 1.01f);
        }
        #endregion
    }    
}
