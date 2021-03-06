﻿// The MIT License (MIT)
//
// Copyright (c) 2013 Jacob Dufault
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Forge.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Forge.Entities.Implementation.Shared {
    /// <summary>
    /// Handles event dispatch. Events are queued up until some point in time and then they are
    /// dispatched.
    /// </summary>
    internal class EventNotifier : IEventNotifier, IEventDispatcher {
        /// <summary>
        /// Event handlers.
        /// </summary>
        private Dictionary<Type, List<Action<object>>> _handlers = new Dictionary<Type, List<Action<object>>>();

        /// <summary>
        /// The queued set of events that have occurred; any thread can write to this list.
        /// </summary>
        private List<IEvent> _events = new List<IEvent>();

        /// <summary>
        /// Events that are currently being dispatched. This is only read from (its values are
        /// retrieved from _events).
        /// </summary>
        private List<IEvent> _dispatchingEvents = new List<IEvent>();

        /// <summary>
        /// Call event handlers for the given event.
        /// </summary>
        /// <param name="eventInstance">The event instance to invoke the handlers for</param>
        private void CallEventHandlers(object eventInstance) {
            List<Action<object>> handlers;
            if (_handlers.TryGetValue(eventInstance.GetType(), out handlers)) {
                for (int i = 0; i < handlers.Count; ++i) {
                    handlers[i](eventInstance);
                }
            }
        }

        /// <summary>
        /// Dispatches all queued events to the registered handlers.
        /// </summary>
        /// <remarks>
        /// One of this methods contracts is that OnEventAdded will not be called while events are
        /// being dispatched.
        /// </remarks>
        internal void DispatchEvents() {
            if (_dispatchingEvents.Count > 0) {
                throw new InvalidOperationException("Dispatch events can only have one caller at a time");
            }

            // swap _events and _dispatchingEvents
            lock (this) {
                Utils.Swap(ref _events, ref _dispatchingEvents);
            }

            // dispatch all events in _dispatchingEvents
            for (int i = 0; i < _dispatchingEvents.Count; ++i) {
                CallEventHandlers(_dispatchingEvents[i]);

                // all event handlers have been called on the event, so the object instance can now
                // be safely reused
                _dispatchingEvents[i].Reuse();
            }
            _dispatchingEvents.Clear();
        }

        /// <summary>
        /// Dispatch an event. Event listeners will be notified of the event at a later point in
        /// time.
        /// </summary>
        /// <param name="evnt">The event instance to dispatch</param>
        public void Submit<TEvent>(TEvent evnt) where TEvent : BaseEvent<TEvent> {
            Log<EventNotifier>.Info("Submitting event " + evnt);
            lock (this) {
                _events.Add(evnt);
            }
        }

        /// <summary>
        /// Add a function that will be called a event of type TEvent has been dispatched to this
        /// dispatcher.
        /// </summary>
        /// <typeparam name="TEvent">The event type to listen for.</typeparam>
        /// <param name="onEvent">The code to invoke.</param>
        public void OnEvent<TEvent>(Action<TEvent> onEvent) where TEvent : BaseEvent<TEvent> {
            Type eventType = typeof(TEvent);

            lock (this) {
                // get our handlers for the given type
                List<Action<object>> handlers;
                if (_handlers.TryGetValue(eventType, out handlers) == false) {
                    handlers = new List<Action<object>>();
                    _handlers[eventType] = handlers;
                }

                // add the handler to the list of handlers
                Action<object> handler = obj => onEvent((TEvent)obj);
                handlers.Add(handler);
            }
        }

        /*
        /// <summary>
        /// Removes an event listener that was previously added with AddListener.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="onEvent"></param>
        /// <returns></returns>
        public bool RemoveListener<TEvent>(Action<TEvent> onEvent) {
            Type eventType = typeof(TEvent);

            lock (this) {
                // get our handlers for the given type
                List<Action<object>> handlers;
                if (_handlers.TryGetValue(eventType, out handlers)) {
                    // removing the handler succeeded
                    throw new NotImplementedException();
                    // return handlers.Remove(onEvent);
                }

                return false;
            }
        }
        */
    }
}