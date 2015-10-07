//*******************************************************************************************************
//  PacketParser.cs
//  Copyright � 2009 - TVA, all rights reserved - Gbtc
//
//  Build Environment: C#, Visual Studio 2008
//  Primary Developer: Pinal C. Patel
//      Office: INFO SVCS APP DEV, CHATTANOOGA - MR BK-C
//       Phone: 423/751-3024
//       Email: pcpatel@tva.gov
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  03/28/2007 - Pinal C. Patel
//       Generated original version of source code.
//  04/21/2009 - Pinal C. Patel
//       Converted to C#.
//
//*******************************************************************************************************

using System;
using TVA.Parsing;

namespace TVA.Historian.Packets
{
    /// <summary>
    /// Represents a data parser that can parse binary data in to <see cref="IPacket"/>s.
    /// </summary>
    public class PacketParser : MultiSourceFrameImageParserBase<Guid, short, IPacket>
    {
        #region [ Properties ]

        /// <summary>
        /// Returns false since the protocol implementation of <see cref="IPacket"/> does not use synchronization.
        /// </summary>
        public override bool ProtocolUsesSyncBytes
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Returns an <see cref="PacketCommonHeader"/> object.
        /// </summary>
        /// <param name="buffer">Buffer containing data to parse.</param>
        /// <param name="offset">Offset index into <paramref name="buffer"/> that represents where to start parsing.</param>
        /// <param name="length">Maximum length of valid data from <paramref name="offset"/>.</param>
        /// <returns>An <see cref="PacketCommonHeader"/> object.</returns>
        protected override ICommonHeader<short> ParseCommonHeader(byte[] buffer, int offset, int length)
        {
            return new PacketCommonHeader(buffer, offset, length);
        }

        #endregion
    }
}