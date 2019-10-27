//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WpfHexaEditor.Core.MethodExtention
{
    public static class TrackExtention
    {
        /// <summary>
        /// Get actual top position of track
        /// </summary>
        public static double Top(this Track track)
        {
            if (track.Parent is Grid parent)
            {
                var topRepeatButton = parent.Children[1] as RepeatButton;

                return topRepeatButton.ActualHeight + parent.Margin.Top + 1;
            }

            return 0;
        }

        /// <summary>
        /// Get actual bottom position of track
        /// </summary>
        public static double Bottom(this Track track)
        {
            if (track.Parent is Grid parent)
            {
                var trackControl = parent.Children[2] as Track;

                return trackControl.Top() +
                       trackControl.ActualHeight +
                       parent.Margin.Top + 1;
            }

            return 0;
        }

        /// <summary>
        /// Get actual bottom position of track
        /// </summary>
        public static double ButtonHeight(this Track track) => track.Top() - 1;

        /// <summary>
        /// Get actual Tick Height
        /// </summary>
        public static double TickHeight(this Track track) => track.ActualHeight / track.Maximum;

        /// <summary>
        /// Get actual Tick Height with another maximum value
        /// </summary>
        public static double TickHeight(this Track track, long maximum) => track.ActualHeight / maximum;
    }
}