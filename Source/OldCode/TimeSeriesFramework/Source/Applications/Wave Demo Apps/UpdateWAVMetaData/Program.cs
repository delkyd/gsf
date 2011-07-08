﻿//******************************************************************************************************
//  Program.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
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
//  06/28/2011 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TVA;
using TVA.Configuration;
using TVA.Data;
using TVA.IO;
using TVA.Media;
using TVA.Units;

namespace UpdateWAVMetaData
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("USAGE:\r\n\r\nUpdateWAVMetaData.exe \"SourcePath\"");
                return 1;
            }

            // System settings
            ConfigurationFile configFile = ConfigurationFile.Current;
            CategorizedSettingsElementCollection systemSettings = configFile.Settings["systemSettings"];
            systemSettings.Add("NodeID", Guid.NewGuid().ToString(), "Unique Node ID");
            Guid nodeID = systemSettings["NodeID"].ValueAs<Guid>();
            string connectionString = systemSettings["ConnectionString"].Value;
            string nodeIDQueryString = null;

            // Define guid with query string delimeters according to database needs
            Dictionary<string, string> settings = connectionString.ParseKeyValuePairs();
            string setting;

            if (settings.TryGetValue("Provider", out setting))
            {
                // Check if provider is for Access since it uses braces as Guid delimeters
                if (setting.StartsWith("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase))
                    nodeIDQueryString = "{" + nodeID + "}";
            }

            if (string.IsNullOrWhiteSpace(nodeIDQueryString))
                nodeIDQueryString = "'" + nodeID + "'";

            AdoDataConnection database = new AdoDataConnection("systemSettings");
            IDbConnection connection = database.Connection;

            if (Convert.ToInt32(connection.ExecuteScalar("SELECT COUNT(*) FROM Protocol WHERE Acronym='WAV'")) == 0)
            {
                try
                {
                    connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, [Type], Category, AssemblyName, TypeName) VALUES('WAV', 'Wave Form Input Adapter', 'Frame', 'Audio', 'WavInputAdapter.dll', 'WavInputAdapter.WavInputAdapter')");
                }
                catch (Exception ex)
                {
                    if (ex.GetType().Name.ToLower().Contains("mysql"))
                        connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName) VALUES('WAV', 'Wave Form Input Adapter', 'Frame', 'Audio', 'WavInputAdapter.dll', 'WavInputAdapter.WavInputAdapter')");
                    else
                        throw ex;
                }
            }

            int protocolID = Convert.ToInt32(connection.ExecuteScalar("SELECT ID FROM Protocol WHERE Acronym='WAV'"));
            int signalTypeID = Convert.ToInt32(connection.ExecuteScalar("SELECT ID FROM SignalType WHERE Acronym='ALOG'"));

            string pathRoot = FilePath.GetDirectoryName(args[0]);
            string sourcePath = pathRoot + "*\\*.wav";

            foreach (string sourceFileName in FilePath.GetFileList(sourcePath))
            {
                WaveFile sourceWave = null;
                string fileName = FilePath.GetFileName(sourceFileName);
                char[] invalidChars = new char[] { '\'', '[', ']', '(', ')', ',', '-', '.' };

                Console.WriteLine("Loading metadata for \"{0}\"...\r\n", fileName);
                sourceWave = WaveFile.Load(sourceFileName, false);

                fileName = FilePath.GetFileNameWithoutExtension(fileName).RemoveDuplicateWhiteSpace().RemoveCharacters(c => invalidChars.Contains(c)).Trim();
                string acronym = fileName.Replace(' ', '_').ToUpper() + "_" + (int)(sourceWave.SampleRate / SI.Kilo) + "KHZ";
                string name = GenerateSongName(sourceWave, fileName);

                Console.WriteLine("   Acronym = {0}", acronym);
                Console.WriteLine("      Name = {0}", name);
                Console.WriteLine("");

                // Check to see if device exists
                if (Convert.ToInt32(connection.ExecuteScalar("SELECT COUNT(*) FROM Device WHERE Acronym=@acronym", acronym)) == 0)
                {
                    // Insert new device record
                    connection.ExecuteNonQuery(string.Format("INSERT INTO Device(NodeID, Acronym, Name, ProtocolID, FramesPerSecond, Enabled) VALUES({0}, @acronym, @name, @protocolID, @framesPerSecond, @enabled )", nodeIDQueryString), acronym, name, protocolID, sourceWave.SampleRate, true);
                    int deviceID = Convert.ToInt32(connection.ExecuteScalar("SELECT ID FROM Device WHERE Acronym=@acronym", acronym));
                    string pointTag, outputMeasurements = "";

                    // Add a measurement for each defined wave channel
                    for (int i = 0; i < sourceWave.Channels; i++)
                    {
                        int index = i + 1;
                        pointTag = acronym + ":WAVA" + index;

                        // Insert new measurement record
                        connection.ExecuteNonQuery("INSERT INTO Measurement(DeviceID, PointTag, SignalTypeID, SignalReference, Description, Enabled) VALUES( @deviceID, @pointTag, @signalTypeID, @signalReference, @description, @enabled )", (object)deviceID, pointTag, signalTypeID, acronym + "-AV" + index, name + " - channel " + index, true);
                        index = Convert.ToInt32(connection.ExecuteScalar("SELECT PointID FROM Measurement WHERE PointTag=@pointTag", pointTag));

                        // Define output measurement keys
                        if (outputMeasurements.Length > 0)
                            outputMeasurements += ";";
                        outputMeasurements += acronym + ":" + index;
                    }

                    // Disable all non analog measurements that may be associated with this device
                    connection.ExecuteNonQuery("UPDATE Measurement SET Enabled=@enabled WHERE DeviceID=@deviceID AND SignalTypeID <> @signalTypeID", false, deviceID, signalTypeID);

                    // Update connection string with newly added measurements
                    connection.ExecuteNonQuery("UPDATE Device SET ConnectionString=@connectionString WHERE ID=@deviceID", string.Format("wavFileName={0}; outputMeasurements={{{1}}}", FilePath.GetAbsolutePath(sourceFileName), outputMeasurements), deviceID);
                }
            }

            connection.Close();

            return 0;
        }

        private static string GenerateSongName(WaveFile song, string fileName)
        {
            Dictionary<string, string> infoStrings = song.InfoStrings;

            string title = GetInfoProperty(infoStrings, "INAM", fileName).Trim();
            string artist = GetInfoProperty(infoStrings, "IART", "").Trim();
            string track = GetInfoProperty(infoStrings, "ITRK", "").Trim();
            string length = " " + song.AudioLength.ToString("hh\\:mm\\:ss");
            string sampleRate = " @ " + (song.SampleRate / SI.Kilo).ToString("0.00kHz");

            if (!string.IsNullOrEmpty(artist))
            {
                if (string.Compare(artist, "Unknown artist", true) == 0)
                    artist = fileName;

                artist = ", " + artist;
            }

            if (!string.IsNullOrEmpty(track))
                track = " #" + track + ",";

            string suffix;

            if (string.IsNullOrEmpty(track) && string.IsNullOrEmpty(artist))
                suffix = sampleRate;
            else
                suffix = " -" + track + length + sampleRate;

            if (title.Length + suffix.Length > 200)
                return title.TruncateRight(199 - suffix.Length) + " " + suffix;
            else if (title.Length + artist.Length + suffix.Length > 200)
                return title + artist.TruncateRight(200 - title.Length - suffix.Length) + " " + suffix;

            return title + artist + suffix;
        }

        private static string GetInfoProperty(Dictionary<string, string> infoStrings, string key, string defaultValue)
        {
            string value;

            if (infoStrings.TryGetValue(key, out value))
            {
                value = value.Trim().RemoveDuplicateWhiteSpace();

                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                return value;
            }

            return defaultValue;
        }
    }
}
