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
	/// Represents a point in the travel path of the bullet. Hit events are sent with BulletPoint objects as arguments, 
	/// They contain information such as the origin & direction of the final raycast that concluded with the hit, or the hit information such as hit type,
	/// point, normal, or bullet information such as travel time or kinetic energy.
	/// Primarily used in Bullet Time to record every point bullet ray has travelled through, so we can actually interpolate a bullet object through these
	/// points in bullet time. Secondarily used in hit events, as mentioned above.
	/// </summary>
	public class BulletPoint
	{
		public BulletProperties m_properties;
		public enum PointHitType { None, Normal, PenetrationIn, PenetrationOut, Ricochet};
		public PointHitType m_hitType;

		/// <summary>
		///  Origin of the ray that caused this point/hit.
		/// </summary>
		public Vector3 m_origin;   

		/// <summary>
		/// End point of the ray that caused this point/hit. If hit type is not None, than this means hit point.
		/// </summary>
		public Vector3 m_endPoint;

		/// <summary>
		/// Hit normal, only if hit type is not None.
		/// </summary>
		public Vector3 m_hitNormal;

		/// <summary>
		/// Direction of the ray that caused this point/hit.
		/// </summary>
		public Vector3 m_direction;

		/// <summary>
		/// Velocity of the bullet.
		/// </summary>
		public Vector3 m_velocity;

		/// <summary>
		/// Hit transform, only if hit type is not None.
		/// </summary>
		public Transform m_hitTransform;

		/// <summary>
		/// Kinetic energy of the bullet.
		/// </summary>
		public float m_kineticEnergy;

		/// <summary>
		/// Time passed since the bullet was fired.
		/// </summary>
		public float m_travelTime;

		/// <summary>
		/// Used internally for Bullet Time Events.
		/// </summary>
		public bool m_isPointAfterTargetHit;
	}

}
