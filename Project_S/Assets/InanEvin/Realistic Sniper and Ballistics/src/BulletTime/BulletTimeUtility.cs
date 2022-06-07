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
    /// Utility methods used by the BulletTimeCameraDefault.
    /// </summary>
    public static class BulletTimeUtility
    {
        /// <summary>
        /// Change this if your game runs on a different timescale than 1.0.
        /// </summary>
        private const float DEFAULT_TIMESCALE = 1.0f;

        public static Vector3 GetRandomVector(Vector3 min, Vector3 max)
        {
            return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
        }

        /// <summary>
        /// DO NOT use this method to change the timescale during bullet time.
        /// During bullet time, the actual timescale will be set to a really low value, so that everything in the world slows down.
        /// The system handles the resetting of actual timescale internally.
        /// </summary>
        /// <param name="ts"></param>
        public static void SetActualTimescale(float ts)
        {
            Time.timeScale = ts;
            Time.fixedDeltaTime *= ts;
        }

        public static void ResetActualTimescale()
        {
            Time.fixedDeltaTime /= Time.timeScale;
            Time.timeScale = DEFAULT_TIMESCALE;
        }

        /// <summary>
        /// USE THIS if you want to play with the timescale affecting the bullet, during bullet time effects.
        /// Bullet time bullet is interpolated by using Time.unscaledTime, so the bullet will not be affected by the actual timescale.
        /// However, it's interpolation is multiplied by the VirtualTimescale, so if you want to give a slow-motion illusion to the bullet, change this.
        /// </summary>
        /// <param name="ts"></param>
        public static void SetVirtualTimescale(float ts)
        {
            SniperAndBallisticsSystem.instance.BulletTimeVirtualTimescale = ts;
        }

        public static void ResetVirtualTimescale()
        {
            SniperAndBallisticsSystem.instance.BulletTimeVirtualTimescale = 1.0f;
        }

        public static float GetRandomNegation()
        {
            return Random.value > 0.5f ? 1.0f : -1.0f;
        }
    }

}
