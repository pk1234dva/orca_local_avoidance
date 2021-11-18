# ORCA local avoidance
For a quick start, just read the section "How to use this package" below. For a summary of what the package does or how it does it, you can read the sections that follow afterwards.

If there are issues or if you have any questions, please let me know on Discord: 
[https://discord.gg/5Mf9bbRkjz](https://discord.gg/5Mf9bbRkjz)

## How to use this package
To start with this package, my suggested approach is to first make a new Unity3D project using the URP Universal Rendering Pipeline, ideally with a 2020.1 Unity3D version or newer. Install the package using the Unity Package Manager - the extracted project should then be located in */Packages/orca\_local\_avoidance/*. Go to */orca\_local\_avoidance/Samples/SamplesUniversalRP* and try running the sample scene inside, checking if there are any errors. The scene shows the basic API of using this package using *AgentSample* and *ProjectileSample* extensions of the *AgentBase* class. While these sample *AgentBase* extensions implement some other logic for updating the agent position/adding forces (*FixedUpdate*), this was only done for simplicity - in a real project, I'd recommend only implementing the necessary abstract methods, keeping the component as modular as possible.

To calculate local avoidance for objects in your scene, add an *AgentBase* extension component to a game object in the scene. These components take care of things like subscribing/unsubscribing to the ORCA manager. Adding these components to your game objects is all that needs to be done - the manager is created and runs automatically, and local avoidance calculations are done exactly for all the active game objects in the scene with an *AgentBase* component, with the simulator assuming that the agents are spheres of *Radius* which can be set under *Agent Base Parameters* in the inspector for all *AgentBase* components.

The resulting "optimized velocity" result will be regularly updated in *AgentBase.OptimizedVelocity*. By modifying your agents' velocity (adding acceleration etc.) to better match the *OptimizedVelocity* value you can avoid collisions between agents to a large extent.

Only sphere based collision avoidance is currently available. This means you should set the radius at least large enough to contain the whole agent. In fact, while *AgentBase.OptimizedVelocity* is guaranteed to avoid collisions under a large amount of circumstances, this assumes the velocity of your agents is matched to this optimized velocity immediately. If you will be using acceleration based modification, you will likely not be able to reach the target velocity immediately. A simple way to account for this delay is to set the radius of the agent to say example 1.5 of the real collider radius. You will have to fiddle around with the value to see what works in your project.

If you don't want to use any local avoidance, you can simply ignore the *OptimizedVelocity* value. You can also turn off all calculation by disabling the manager, i.e. *OrcaManager.Instance.enabled=false*, though it's not necessary as the overhead should be minimal if left running and simply ignoring the *OptimizedVelocity* value. 

The *OrcaManager* object should not be deleted, so make sure to avoid that. It will be under the *DontDestroyOnLoad* object at runtime, so simply avoiding deleting children of this object should be enough.

## Implementing your own *AgentBase* extensions
If you simply want to go ahead and use the package and not bother with understanding how it works, then all you need to know is how to make your own extensions of the *AgentBase* class that you can find in */orca\_local\_avoidance/LowLevel/*. You can take a look at the simple implemention in *AgentSample* that assumes you are using a *Rigidbody* to control your agent.

When making your own extensions of this class, you need to implement three methods:

 * *UpdatePosition* - this method should return the current position of the agent,
 * *UpdateVelocity* - this method should return the current velocity of the agent,
 * *UpdateTargetVelocity* - this method should return the "ideal" velocity of the agent, for example the agent might have a target position it wants to reach - in that case, it might make sense that its "ideal" velocity is the direction towards the target times the maximum possible speed.


The simulator that runs the calculations for local avoidance needs to know the above values and it gets them by using these methods.

If you then add *AgentBase* extensions to objects in your scene, *AgentBase.OptimizedVelocity* will be regularly updated.

Currently the agents subscribe and unsubscribe automatically using the *OnEnable* and *OnDisable* Unity messages (search for *OnEnable* in *AgentBase.cs*), and it's this subscribing / unsubscribing that determines whether the agents are used in the simulator calculations and their *OptimizedVelocity* value is updated.

## Simulator settings
When the simulator is created, it attempts to read an *OrcaSettings* scriptable object, that contains settings. You can make your own settings and modify the amount of threads that will be used and the frequency at which the simulation computations are done. This file has to be placed in a "Resources" folder, and I recommend placing it inside */orca\_local\_avoidance/Runtime/Resources/*.

## Advanced settings
It's not inherently clear how much each agent is "responsible" for avoiding other agents. The default is that each agent assumes that itself and the other agent are both responsible for half of the avoidance. The desired result is standardized to a number between 0 and 1, 1 implying that the agent will optimize its velocity to avoid the other agent, and assuming it's fully responsible for avoiding a collision (e.g. agent trying to avoidance a projectile). 

Under *Avoidance parameters* in the *AgentBase* component in the inspector, I have added various options that allows customization of this behavior. Here's a quick explanation of how they work, though it might be best to simply take a look at the code - you can find it in *AgentBase.cs* in the *CalculateRelativeWeight* method:

**AgentType**:

 * *Sentient* - standard agent, dodges to some extent depending on their type and other parameters.
 * *Nonsentient* - this is meant to be used for objects like projectiles - these agents do not get their optimized velocities calculated, but they are considered as something to be avoided by others. Note that *AgentBase.UpdateTargetVelocity* is irrelevant in this case and you can set it to anything you want (it still needs to be implemented though).



**Dodge-type**:
Used when a sentient agent is trying to avoid a non-sentient type. 

 * *Treat projectile as if sentient* - does what the name implies.
 * *None* - does what the name implies.
 * *Fixed* - does what the name implies - takes *dodgeWeight* amount of responsibility for avoiding the projectile.
 * *Standard* determines the responsibility as *this.dodgeWeight * other.Weight* - for example if the projectile is of weight 100, and this agent has a *dodgeWeight* of 0.01, we have 100 * 0.01 = 1 responsibility. If the weight of the projectile is lowered, the agent will not try to move out of the way as quickly.


**Weight type**:
This is used in the case that the other agent is sentient, or if the other agent is non-sentient but we're using *dodgeType* == "Treat projectile as if sentient" (see above). 

 * *Fixed* means the weight will be clamped to [0, 1] and used directly. "linear interpolation" determines the responsibility as a linear interpolation between the weight of this and the other agent.
 * *Linear interpolation* determines the responsibility as a weighted linear interpolation between the weight of this and the other agent.


Example of using *Linear interpolation* - suppose our agent has a weight of 1000, and the other agent has a weight of 100. Then this agent will be responsible for 100/(100+1000) of the avoidance and the other agent is responsible for 1000 / (100+ 1000) of the avoidance. This behavior corresponds to a case where we have say a large spaceship avoiding a small one - intuitively it makes sense that it will be mostly the small spaceship that will try to dodge the large one, while the large one will not do much work.

## About
This Unity3D package offers a basic local avoidance solution using ORCA. It's a c\# extension of the RVO2-3D Library c++ project that you can find at [https://gamma.cs.unc.edu/RVO2/](https://gamma.cs.unc.edu/RVO2/), using the ORCA local avoidance approach by Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, and Dinesh Manocha. There is however a large amount of modifications in the higher level parts of the project and various re-organization and renaming. The linear programs and most of the ORCA plane creations  have been kept, with the exception being the projection onto the cone - there were some numerical errors and NaNs with the original implementation of cone projection, and I wasn't able to decode the original code to fix it - see "Project on cone" in *AgentBase*. Please let me know if you have any reference to the approach that was used, or you understand it - I would be happy to try and fix it, rather than use my own solution.

The approach is velocity based - this means that only velocities and positions of the objects are used, and the resulting "optimized" value is also a velocity. The user has to decide themselves how they will modify the real velocity using this "optimized" value.

Internally, the simulator which runs the calculations is created lazily and follows a singleton pattern. While I tried to avoid this approach to allow for more flexibility, due to the need of creating threads, running specific code upon initialization, and regular updates to the agents, other methods like using scriptable objects/scene-independent solutions seemed problematic. If there is demand for a different implementation and there is a valid reason, I will modify this and allow for a different approach, but as of now, it's too difficult for me to reason about what might be useful, and I don't want to waste time on adding flexibility that will ultimately never be used.

The simulator is multi-threaded, creating its own thread-pool like object, and should not allocate any garbage. Overall the project has been made in a way that should have minimal garbage allocation and minimal processing done in the main Unity thread, avoiding any possible hold-ups (assuming you do not set the simulator update frequency too high). Please let me know if you notice any regular garbage allocation, as that should not happen.

## Low-level API
For those interested, here's a quick outline of how it all works.

*Simulator* contains the actual code that initializes and controls the threads that run the calculations. The crucial method is *MainWork*, which will be run by the "main worker" thread. It updates the *KdTree*, controls the other worker threads, and does some calculations itself.
Internally there is a *agentsSet* HashSet that holds the set of agents - it's this object into which agents are actually added. The List of agents is regularly updated based on this set, and then used in calculations.
Each *MainWork* iteration essentially iterates across all the agents and updates their optimized velocities - the threads simply split the indices that they are responsible for.

*Worker* - a basic worker object that runs the *Worker.Work* thread loop. Similar to a thread pool, it uses a while(true) loop together with conditional variables to start/stop execution.

*OrcaManager* acts as a wrapper around Simulator - it's a *MonoBehaviour*, and it uses the *Update* Unity message to regularly start/wait for the threads, as well as update the internal agents List in a thread safe way - the list should not be modified while computation could be in process, though adding/removing from the *agentsSet* HashSet is safe at any time.

*KdTree* contains a kd-tree data structure that is used for efficient neighbor search. When calculating local avoidance we ignore certain neighbors that are too far away, and to quickly find these we use a kd tree. It's rebuilt once during each *Simulator.MainWork* call and then used by all the agents.

*AgentBase.cs* is a base abstract class that contains all the possible agent parameters and code for velocity optimization code.

## Future plans and various implementation notes

 * Add an option to use agents that will avoid other agents, but limit their movement to a plane (example use case - non-flying agents that will be able to avoid flying / non flying agents). Currently your only option for this type of behaviour is to project the resulting *OptimizedVelocity*, but this will not always give correct result.
 * Add other possible manager options? Standardized way to turn off that simulator?
 * Once all of that is done done and the project is stable, convert to Job System.

___
2021 Jakub Prokop