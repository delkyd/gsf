//******************************************************************************************************
//  UniversalTags.cs - Gbtc
//
//  Copyright � 2013, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  09/24/2013 - J. Ritchie Carroll
//       Derived original version of source code from BinaryNotes (http://bnotes.sourceforge.net).
//
//******************************************************************************************************

#region [ Contributor License Agreements ]

/*
    Copyright 2006-2011 Abdulla Abdurakhmanov (abdulla@latestbit.com)
    Original sources are available at www.latestbit.com

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

#endregion

namespace GSF.ASN1.Coders
{
    public struct UniversalTags
    {
        public const int Reserved0 = 0;
        public const int Boolean = 1;
        public const int Integer = 2;
        public const int Bitstring = 3;
        public const int OctetString = 4;
        public const int Null = 5;
        public const int ObjectIdentifier = 6;
        public const int ObjectDescriptor = 7;
        public const int External = 8;
        public const int Real = 9;
        public const int Enumerated = 10;
        public const int EmbeddedPdv = 11;
        public const int UTF8String = 12;
        public const int RelativeObject = 13;
        public const int Reserved14 = 14;
        public const int Reserved15 = 15;
        public const int Sequence = 16;
        public const int Set = 17;
        public const int NumericString = 18;
        public const int PrintableString = 19;
        public const int TeletexString = 20;
        public const int VideotexString = 21;
        public const int IA5String = 22;
        public const int UTCTime = 23;
        public const int GeneralizedTime = 24;
        public const int GraphicString = 25;
        public const int VisibleString = 26;
        public const int GeneralString = 27;
        public const int UniversalString = 28;
        public const int UnspecifiedString = 29;
        public const int BMPString = 30;
        public const int LastUniversal = 31;
    }
}