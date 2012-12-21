﻿//******************************************************************************************************
//  MasterConfiguration.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/05/2012 - Adam Crain
//       Generated original version of source code.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header. 
//
//******************************************************************************************************

using DNP3.Interface;
using System;

namespace Dnp3Adapters
{
    public class MasterConfiguration
    {
        /// <summary>
        /// All of the settings for the connection
        /// </summary>
        public TcpClientConfig client = new TcpClientConfig();

        /// <summary>
        /// All of the settings for the master
        /// </summary>
        public MasterStackConfig master = new MasterStackConfig();
    }

    public class TcpClientConfig
    {
        /// <summary>
        /// IP address of host
        /// </summary>
        public String address = "127.0.0.1";

        /// <summary>
        /// TCP port for connection
        /// </summary>
        public UInt16 port;

        /// <summary>
        /// Connection retry interval in milliseconds
        /// </summary>
        public UInt64 retryMs;

        /// <summary>
        /// DNP3 filter level for port messages
        /// </summary>
        public FilterLevel level;
    }
}
