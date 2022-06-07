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
using UnityEngine.EventSystems;

namespace IE.RSB
{
    /// <summary>
    /// Joystick event listener for movement & look joysticks, used for example mobile joystick canvas.
    /// DemoMobileControls script checks the event position object & determines the inputs, as well as the knob's positions.
    /// </summary>
	public class DemoMobileJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{

        private Vector2 m_eventPosition = Vector2.zero;
        private Vector2 m_eventPositionWhenDragBegins = Vector2.zero;

        public Vector2 GetEventPosition() { return m_eventPosition; }
        public Vector2 GetDragBeginPosition() { return m_eventPositionWhenDragBegins; }

		public void OnBeginDrag(PointerEventData eventData)
		{
            m_eventPositionWhenDragBegins = eventData.position;
		}
        public void OnDrag(PointerEventData eventData)
        {
            m_eventPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_eventPosition = Vector2.zero;
        }
 
    }

}
