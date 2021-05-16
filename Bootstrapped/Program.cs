﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Program.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bootstrapped {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using NLog;
    using NLog.Config;

    using Sharlayan;
    using Sharlayan.Events;
    using Sharlayan.Models;
    using Sharlayan.Models.XIVDatabase;
    using Sharlayan.Utilities;

    class Program {
        static void Main(string[] args) {
            StringReader stringReader = new StringReader(XElement.Load("./Bootstrapped.exe.nlog").ToString());

            using (XmlReader xmlReader = XmlReader.Create(stringReader)) {
                LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);
            }

            ActionLookup.GetActionInfo(2);
            StatusEffectLookup.GetStatusInfo(2);
            ZoneLookup.GetZoneInfo(138);

            ActionItem action = ActionLookup.GetActionInfo(2);
            StatusItem status = StatusEffectLookup.GetStatusInfo(2);
            MapItem zone = ZoneLookup.GetZoneInfo(138);

            Process process = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();

            if (process != null) {
                Console.WriteLine("setting up memory handler...");

                SharlayanConfiguration configuration = new SharlayanConfiguration {
                    ProcessModel = new ProcessModel {
                        Process = process,
                    },
                };
                MemoryHandler memoryHandler = new MemoryHandler(configuration);

                Console.WriteLine("scanning for memory locations...");

                memoryHandler.ExceptionEvent += delegate(object? sender, ExceptionEvent e) {
                    Console.WriteLine(e.Exception.Message);
                };
                memoryHandler.MemoryLocationsFoundEvent += delegate(object sender, MemoryLocationsFoundEvent e) {
                    Console.WriteLine("memory locations found...");
                    foreach (KeyValuePair<string, MemoryLocation> kvp in e.MemoryLocations) {
                        Console.WriteLine($"{kvp.Key} => {kvp.Value.GetAddress():X}");
                    }
                };

                while (memoryHandler.Scanner.IsScanning) {
                    Thread.Sleep(100);
                }

                Console.WriteLine("completed...");

                memoryHandler.Reader.GetPartyMembers();
                memoryHandler.Reader.GetActions();
                memoryHandler.Reader.GetActors();
                memoryHandler.Reader.GetChatLog();
                memoryHandler.Reader.GetCurrentPlayer();
                memoryHandler.Reader.GetInventory();
                memoryHandler.Reader.GetJobResources();
                memoryHandler.Reader.GetTargetInfo();

                Console.WriteLine("To exit this application press \"Enter\".");
            }
        }
    }
}