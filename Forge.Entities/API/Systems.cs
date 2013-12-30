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

using Newtonsoft.Json;
using System;

namespace Forge.Entities {
    /// <summary>
    /// All systems need to extend this interface, but it should be done by extending BaseSystem.
    /// See documentation on BaseSystem.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public interface ISystem {
        /// <summary>
        /// Set the event dispatcher that can be used to notify the external world of events.
        /// </summary>
        IEventDispatcher EventDispatcher {
            set;
        }

        /// <summary>
        /// Set the global entity that can be used to store global data.
        /// </summary>
        IEntity GlobalEntity {
            set;
        }
    }

    /// <summary>
    /// All systems need to extend the system class. Systems have callbacks automatically registered
    /// by implementing ITrigger* interfaces.
    /// </summary>
    /// <remarks>
    /// Client code should not directly extend this, as it does not give any behavior by itself.
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseSystem : ISystem {
        /// <summary>
        /// Get the event dispatcher that can be used to notify the external world of events.
        /// </summary>
        protected internal IEventDispatcher EventDispatcher {
            get;
            private set;
        }

        IEventDispatcher ISystem.EventDispatcher {
            set {
                EventDispatcher = value;
            }
        }

        /// <summary>
        /// Get the global entity that can be used to store global data.
        /// </summary>
        protected internal IEntity GlobalEntity {
            get;
            private set;
        }

        IEntity ISystem.GlobalEntity {
            set {
                GlobalEntity = value;
            }
        }
    }

    /// <summary>
    /// * do not extend this type; it does not provide any functionality *
    /// A base type for triggers which require a filter that exposes the common RequiredDataTypes
    /// method.
    /// </summary>
    /// <remarks>
    /// Client code should not extend this.
    /// </remarks>
    public interface ITriggerFilterProvider {
        /// <summary>
        /// Computes the entity filter.
        /// </summary>
        /// <remarks>
        /// Entities, by default, pass the filter. They pass the filter when we can prove they don't
        /// belong, ie, they lack one of the data types in the entity filter. So, if the filter is
        /// empty, then every entity will be within the filter.
        /// </remarks>
        /// <returns>A list of Data types that the entity needs to have to pass the
        /// filter.</returns>
        Type[] RequiredDataTypes();
    }

    /// <summary>
    /// Provides interfaces that Systems should derive from to receive callbacks.
    /// </summary>
    public static class Trigger {
        /// <summary>
        /// Adds an OnAdded method to the system, which is called when the entity has passed the
        /// given filter.
        /// </summary>
        public interface Added : ISystem, ITriggerFilterProvider {
            /// <summary>
            /// Called when an Entity has passed the filter.
            /// </summary>
            /// <param name="entity">An entity that is now passing the filter.</param>
            void OnAdded(IEntity entity);
        }

        /// <summary>
        /// Adds an OnRemoved method to the system, which is called when an entity no longer passes
        /// the given filter after it has passed it.
        /// </summary>
        public interface Removed : ISystem, ITriggerFilterProvider {
            /// <summary>
            /// Called when an Entity, which was once passing the filter, is no longer doing so.
            /// </summary>
            /// <remarks>
            /// This can occur for a number of reasons, such as a data state change or the Entity
            /// being destroyed.
            /// </remarks>
            /// <param name="entity">An entity that is no longer passing the filter.</param>
            void OnRemoved(IEntity entity);
        }

        /// <summary>
        /// Adds an OnModified method to the system, which is called whenever an entity which passes
        /// the filter is modified.
        /// </summary>
        public interface Modified : ISystem, ITriggerFilterProvider {
            /// <summary>
            /// The given entity, which has passed the filter, has been modified.
            /// </summary>
            /// <param name="entity">An entity which has passed the filter.</param>
            void OnModified(IEntity entity);
        }

        /// <summary>
        /// Adds an OnUpdate method to the system, which is called on every entity that passes the
        /// filter each update.
        /// </summary>
        public interface Update : ISystem, ITriggerFilterProvider {
            /// <summary>
            /// This is called every update frame for all entities which pass the filter.
            /// </summary>
            /// <remarks>
            /// If you need to know when the entities are added or are no longer going to be
            /// updated, also implement ILifecycleTrigger.
            /// </remarks>
            /// <param name="entity">An entity which has passed the filter.</param>
            void OnUpdate(IEntity entity);
        }

        /// <summary>
        /// Adds an OnGlobalPreUpdate method to the system, which is called before OnUpdate has
        /// started.
        /// </summary>
        public interface GlobalPreUpdate : ISystem {
            /// <summary>
            /// Called once per update loop. This is expected to use the EntityManager's global
            /// data.
            /// </summary>
            void OnGlobalPreUpdate();
        }

        /// <summary>
        /// Adds an OnGlobalPostUpdate method to the system, which is called after OnUpdate has
        /// completed for this system.
        /// </summary>
        public interface GlobalPostUpdate : ISystem {
            /// <summary>
            /// Called once per update loop. This is expected to use the EntityManager's global
            /// data.
            /// </summary>
            void OnGlobalPostUpdate();
        }

        /// <summary>
        /// Adds an OnInput method to the system, which is called on every entity that passes the
        /// filter when one of the given input types has been received by the game engine.
        /// </summary>
        public interface Input : ISystem, ITriggerFilterProvider {
            /// <summary>
            /// The types of game input that the trigger is interested in.
            /// </summary>
            Type[] InputTypes {
                get;
            }

            /// <summary>
            /// Called on all entities which pass the filter.
            /// </summary>
            /// <param name="input">The input that was received.</param>
            /// <param name="entity">An entity which has passed the filter.</param>
            void OnInput(IGameInput input, IEntity entity);
        }

        /// <summary>
        /// Adds an OnGlobalInput method to the system, which is called when one of the given input
        /// types has been received by the game engine.
        /// </summary>
        public interface GlobalInput : ISystem {
            /// <summary>
            /// The types of game input that the trigger is interested in.
            /// </summary>
            Type[] InputTypes {
                get;
            }

            /// <summary>
            /// Called whenever the given input type is received.
            /// </summary>
            /// <param name="input">The input that was received.</param>
            void OnGlobalInput(IGameInput input);
        }
    }
}