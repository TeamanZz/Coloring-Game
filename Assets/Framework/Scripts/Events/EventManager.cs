using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	/********************************************************/
	/* NOTE:
	/* Only register for events in your classes Start method!
	 * Events that are sent in the Start method will not be passed along to handlers until the for Update loop.
	/********************************************************/
	public abstract class EventManager<T> : SingletonComponent<T> where T : Object
	{
		#region Classes

		private class EarlyEvent
		{
			public string	eventId	= "";
			public object[]	data	= null;
		}

		#endregion

		#region Member Variables

		private Dictionary<string, List<System.Type>>	eventDataTypes;
		private Dictionary<string, List<EventHandler>>	eventHandlers;
		private List<EarlyEvent>						earlyEvents;
		private bool									updateLoopStarted;

		#endregion

		#region Delegates

		public delegate void EventHandler(string eventId, object[] data);

		#endregion

		#region Abstract Methods

		protected abstract Dictionary<string, List<System.Type>> GetEventDataTypes();

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			eventHandlers		= new Dictionary<string, List<EventHandler>>();
			earlyEvents			= new List<EarlyEvent>();
			updateLoopStarted	= false;

			eventDataTypes = GetEventDataTypes();
		}

		private void Update()
		{
			if (!updateLoopStarted)
			{
				updateLoopStarted = true;

				for (int i = 0; i < earlyEvents.Count; i++)
				{
					SendEvent(earlyEvents[i].eventId, earlyEvents[i].data);
				}

				earlyEvents.Clear();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Registers an EventHandler callback to be called when the eventId event is sent.
		/// </summary>
		public void RegisterEventHandler(string eventId, EventHandler eventHandler)
		{
			if (!eventHandlers.ContainsKey(eventId))
			{
				eventHandlers.Add(eventId, new List<EventHandler>());
			}

			eventHandlers[eventId].Add(eventHandler);
		}

		/// <summary>
		/// Uns the register event handler.
		/// </summary>
		public void UnRegisterEventHandler(string eventId, EventHandler eventHandler)
		{
			if (eventHandlers.ContainsKey(eventId))
			{
				eventHandlers[eventId].Remove(eventHandler);
			}
		}

		/// <summary>
		/// Sends an event to all register EventHandlers for the eventId.
		/// </summary>
		public void SendEvent(string eventId, params object[] data)
		{
			if (!eventHandlers.ContainsKey(eventId))
			{
				return;
			}

			// Check if the data contains the exected types
			if (!Check(eventId, data))
			{
				return;
			}

			// We cannot send events until the first Update loop because if an event is fired before a system can register it in it's Start method then it will be missed.
			if (!updateLoopStarted)
			{
				EarlyEvent earlyEvent	= new EarlyEvent();
				earlyEvent.eventId		= eventId;
				earlyEvent.data			= data;

				earlyEvents.Add(earlyEvent);
			}
			else
			{
				// Call all event handlers that have registered for the event
				List<EventHandler> eventHandlerList = eventHandlers[eventId];

				for (int i = 0; i < eventHandlerList.Count; i++)
				{
					eventHandlerList[i](eventId, data);
				}
			}
		}

		#endregion

		#region Private Methods

		private bool Check(string eventId, object[] data)
		{
			if (!eventDataTypes.ContainsKey(eventId))
			{
				Debug.LogError("[EventManager] Event Id does not exists in the data types dictionary, eventId: " + eventId);
				return false;
			}

			List<System.Type> dataTypes = eventDataTypes[eventId];

			if (dataTypes.Count != data.Length)
			{
				Debug.LogError("[EventManager] Number of data items does not match number of types the event is expecting, eventId: " + eventId);
				return false;
			}

			for (int i = 0; i < dataTypes.Count; i++)
			{
				if (dataTypes[i] != data[i].GetType())
				{
					Debug.LogError("[EventManager] Mismatched data type for event, eventId: " + eventId);
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}
