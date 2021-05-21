﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatLogWorker.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ChatLogWorker.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace BootstrappedWPF.SharlayanWrappers.Workers {
    using System;
    using System.Threading.Tasks;
    using System.Timers;

    using BootstrappedWPF.Properties;

    using Sharlayan;
    using Sharlayan.Core;
    using Sharlayan.Models.ReadResults;

    internal class ChatLogWorker : PropertyChangedBase, IDisposable {
        private readonly MemoryHandler _memoryHandler;

        private readonly Timer _scanTimer;

        private bool _isScanning;

        private int _previousArrayIndex;

        private int _previousOffset;

        public ChatLogWorker(MemoryHandler memoryHandler) {
            this._memoryHandler = memoryHandler;
            this._scanTimer = new Timer(250);
            this._scanTimer.Elapsed += this.ScanTimerElapsed;
        }

        ~ChatLogWorker() {
            this.Dispose();
        }

        public void Dispose() {
            this._scanTimer.Elapsed -= this.ScanTimerElapsed;
        }

        public void Reset() {
            this._previousArrayIndex = 0;
            this._previousOffset = 0;
        }

        public void StartScanning() {
            this._scanTimer.Enabled = true;
        }

        public void StopScanning() {
            this._scanTimer.Enabled = false;
        }

        private void ScanTimerElapsed(object sender, ElapsedEventArgs e) {
            if (this._isScanning) {
                return;
            }

            this._scanTimer.Interval = Settings.Default.ChatLogWorkerTiming;

            this._isScanning = true;

            Task.Run(
                () => {
                    ChatLogResult result = this._memoryHandler.Reader.GetChatLog(this._previousArrayIndex, this._previousOffset);

                    this._previousArrayIndex = result.PreviousArrayIndex;
                    this._previousOffset = result.PreviousOffset;

                    while (!result.ChatLogItems.IsEmpty) {
                        if (result.ChatLogItems.TryDequeue(out ChatLogItem chatLogItem)) {
                            EventHost.Instance.RaiseNewChatLogItemEvent(this._memoryHandler, chatLogItem);
                        }
                    }

                    this._isScanning = false;
                });
        }
    }
}