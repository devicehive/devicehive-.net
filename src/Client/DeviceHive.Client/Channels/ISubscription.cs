using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a subscription for notifications and commands.
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        /// Gets unique subscription identifier.
        /// </summary>
        Guid Id { get; }
        
        /// <summary>
        /// Gets subscription type.
        /// </summary>
        SubscriptionType Type { get; }

        /// <summary>
        /// Gets the list of target device unique identifiers.
        /// If null, subscription was made to all available devices.
        /// </summary>
        string[] DeviceGuids { get; }

        /// <summary>
        /// Gets the list of target event names.
        /// If null, subscription was made to all available events.
        /// </summary>
        string[] EventNames { get; }
    }
}
