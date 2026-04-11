/*
 * Date: 2013-02-03
 * Sources used: 
 *  http://xiph.org/flac/format.html#seekpoint
 *  http://py.thoulon.free.fr/
 */
namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

/// <summary>
/// 
/// </summary>
public class FlacSeekTableMetadataBlock : FlacMetadataBlock
{
    private readonly EventList<FlacSeekPoint> _seekPoints = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="FlacSeekTableMetadataBlock"/> class.
    /// </summary>
    public FlacSeekTableMetadataBlock()
    {
        _seekPoints.ItemAdd += SeekPointAdd;

        _seekPoints.ItemReplace += SeekPointReplace;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override FlacMetadataBlockType BlockType
    {
        get
        {
            return FlacMetadataBlockType.SeekTable;
        }
    }

    /// <inheritdoc/>
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            foreach (var seekPoint in _seekPoints)
            {
                stream.WriteBigEndianInt64(seekPoint.SampleNumber);
                stream.WriteBigEndianInt64(seekPoint.Offset);
                stream.WriteBigEndianInt16((short)seekPoint.Samples);
            }
            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            while (stream.Position < stream.Length)
            {
                _seekPoints.Add(
                    new FlacSeekPoint(
                        stream.ReadBigEndianInt64(), stream.ReadBigEndianInt64(), stream.ReadBigEndianInt16()));
            }
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the seek table.
    /// </summary>
    /// <value>
    /// The seek table.
    /// </value>
    public IEnumerable<FlacSeekPoint> SeekTable
    {
        get
        {
            return _seekPoints.AsReadOnly();
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private void SeekPointAdd(object? sender, ListItemAddEventArgs<FlacSeekPoint> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Item == null)
        {
            throw new NullReferenceException("e.Item may not be null");
        }

        // Seek points within a table must be unique by sample number, with the exception of placeholder points.
        for (var i = 0; i < _seekPoints.Count; i++)
        {
            var seekPoint = _seekPoints[i];

            // Seek points within a table must be sorted in ascending order by sample number.
            if (seekPoint.SampleNumber >= e.Item.SampleNumber)
            {
                e.Index = i;
                break;
            }
        }
    }

    private void SeekPointReplace(object? sender, ListItemReplaceEventArgs<FlacSeekPoint> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.NewItem == null)
        {
            throw new NullReferenceException("e.NewItem may not be null");
        }

        _seekPoints.RemoveAt(e.Index);
        e.Cancel = true;
        _seekPoints.Add(e.NewItem);
    }
}
