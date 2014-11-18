using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents event arguments to the channel state changed event.
    /// </summary>
    public class ChannelStateEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets old channel state.
        /// </summary>
        public ChannelState OldState { get; private set; }

        /// <summary>
        /// Gets new channel state.
        /// </summary>
        public ChannelState NewState { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="oldState">Old channel state.</param>
        /// <param name="newState">New channel state.</param>
        public ChannelStateEventArgs(ChannelState oldState, ChannelState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
        #endregion
    }
}
