//*******************************************************************************************************
//  IBasicParser.cs
//  Copyright © 2008 - TVA, all rights reserved - Gbtc
//
//  Build Environment: C#, Visual Studio 2008
//  Primary Developer: James R Carroll
//      Office: PSO TRAN & REL, CHATTANOOGA - MR BK-C
//       Phone: 423/751-4165
//       Email: jrcarrol@tva.gov
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  12/03/2008 - James R Carroll
//       Generated original version of source code.
//
//*******************************************************************************************************

using System;

namespace PCS.Parsing
{
    /// <summary>
    /// This interface represents the protocol independent representation of a binary image parser suitable for common,
    /// simple formatted, binary data streams returning the parsed data via events
    /// </summary>
    /// <typeparam name="TTypeIdentifier">Type of identifier used to distinguish output types.</typeparam>
    /// <typeparam name="TOutputType">Type of the interface or class used to represent outputs.</typeparam>
    public interface IBasicParser<TTypeIdentifier, TOutputType> : IStreamParser
    {
        /// <summary>
        /// Occurs when a data image is deserialized successfully to one of the output types that the data
        /// image represented.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the object that was deserialized from the binary image.
        /// </remarks>
        event EventHandler<EventArgs<TOutputType>> DataParsed;

        /// <summary>
        /// Occurs when matching a output type for deserializing the data image cound not be found.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the ID of the output type that could not be found.
        /// </remarks>
        event EventHandler<EventArgs<TTypeIdentifier>> OutputTypeNotFound;
    }
}