/*
 * Date: 2013-09-28
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
using System;

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        private void BindFrameEvents()
        {
            _frames.ItemAdd += FrameAdd;

            _frames.ItemReplace += FrameReplace;

            _frames.ItemRemove += FrameRemove;
        }

        private void UnbindFrameEvents()
        {
            _frames.ItemAdd -= FrameAdd;

            _frames.ItemReplace -= FrameReplace;

            _frames.ItemRemove -= FrameRemove;
        }

        private void FrameAdd(object sender, ListItemAddEventArgs<Id3v2Frame> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            string identifier = Id3v2Frame.GetIdentifier<Id3v2MusicCdIdentifierFrame>(e.Item.Version);
            if ((e.Item is Id3v2MusicCdIdentifierFrame || String.Equals(e.Item.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                && TrackNumber == null)
            {
                // The 'Music CD Identifier' frame requires a present and valid TRCK frame.
                throw new InvalidOperationException(
                    "TrackNumber frame is required to be present and valid before adding a MusicCdIdentifier frame, even if the CD's only got one track.");
            }

            if (Version >= Id3v2Version.Id3v240)
            {
                identifier = Id3v2Frame.GetIdentifier<Id3v2AudioSeekPointIndexFrame>(e.Item.Version);
                if ((e.Item is Id3v2AudioSeekPointIndexFrame || String.Equals(e.Item.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                    && Length == null)
                {
                    // The presence of an 'Audio seek point index' frame requires the existence of a TLEN frame, indicating the duration of the file in milliseconds.
                    throw new InvalidOperationException("Length frame is required to be present before adding an audio seek point index frame.");
                }
            }
        }

        private void FrameReplace(object sender, ListItemReplaceEventArgs<Id3v2Frame> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            if (String.Equals(e.OldItem.Identifier, e.NewItem.Identifier) || (e.OldItem.GetType() == e.NewItem.GetType()))
                return;

            FrameRemove(sender, new ListItemRemoveEventArgs<Id3v2Frame>(e.OldItem));
            FrameAdd(sender, new ListItemAddEventArgs<Id3v2Frame>(e.NewItem));
        }

        private void FrameRemove(object sender, ListItemRemoveEventArgs<Id3v2Frame> e)
        {
            if ((e == null) || (e.Item == null))
                return;

            if (String.Equals(e.Item.Identifier, Id3v2TextFrame.GetIdentifier(Version, Id3v2TextFrameIdentifier.Length)))
            {
                // The presence of an 'Audio seek point index' frame requires the existence of a TLEN frame, indicating the duration of the file in milliseconds.
                if (AudioSeekPointIndex != null)
                    throw new InvalidOperationException("The AudioSeekPointIndex frame needs to be removed before the Length frame can be removed.");
            }
            else if (String.Equals(e.Item.Identifier, Id3v2TextFrame.GetIdentifier(Version, Id3v2TextFrameIdentifier.AlbumSortOrder)))
            {
                // The 'Music CD Identifier' frame requires a present and valid TRCK frame.
                if (MusicCdIdentifier != null)
                {
                    throw new InvalidOperationException(
                        "The music CD identifier frame needs to be removed before the TrackNumber frame can be removed.");
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void ClearFrames()
        {
            // Remove frame events, clear all frames, and add the frame events again. This so we can clear the events without triggering stuff.
            UnbindFrameEvents();
            _frames.Clear();
            BindFrameEvents();
        }

        private void ValidateFrames()
        {
            if ((MusicCdIdentifier != null) && (TrackNumber == null))
            {
                // The 'Music CD Identifier' frame requires a present and valid TRCK frame.
                throw new InvalidOperationException(
                    "TrackNumber frame is required to be present and valid before adding a MusicCdIdentifier frame, even if the CD's only got one track.");
            }

            if (Version >= Id3v2Version.Id3v240)
            {
                if ((AudioSeekPointIndex != null) && (Length == null))
                {
                    // The presence of an 'Audio seek point index' frame requires the existence of a TLEN frame, indicating the duration of the file in milliseconds.
                    throw new InvalidOperationException("Length frame is required to be present before adding an audio seek point index frame.");
                }
            }
        }
    }
}
