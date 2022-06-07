/*
 * Realistic Sniper and Ballistics System
 * Copyright (c) 2021, Inan Evin, Inc. All rights reserved.
 * Author: Inan Evin
 * https://www.inanevin.com/
 * 
 * Documentation: https://rsb.inanevin.com
 * Contact: inanevin@gmail.com
 * Support Discord: https://discord.gg/RCzpSAmBAb
 *
 * Feel free to ask about RSB, send feature recommendations or any other feedback!
 */

using UnityEngine;

namespace IE.RSB
{
	/// <summary>
	/// Normally when an object is hit, the object's tag or layer is checked whether to understand if the object represents
	/// a dirt, metal, plastic, grass etc. surface. So that according to the tag, users can spawn different hit particles.
	/// However in RSB we did not want to mess with user's project settings, so we did not introduce any tags.
	/// Instead, we simply used HitTagBody, HitTagMetal and HitTagWood scripts to determine what type of a surface we are hitting
	/// so we can spawn different particle effects. HitListenerSpawnParticle.cs script listens to hits, and if the hit object contains
	/// any of the hit tags, it spawns the particles accordingly.
	/// In your own projects, you can simply write your own particle spawner (instead of using HitListenerSpawnParticle.cs) 
	/// and check for the surfaces based on tags or layers, depending on your project.
	/// </summary>
	public class HitTagBody : MonoBehaviour
	{

	}

}
