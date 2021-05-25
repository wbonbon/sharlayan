﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartyMemberResolver.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson <syndicated.life@gmail.com> (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   PartyMemberResolver.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NLog;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;
    using Sharlayan.Delegates;
    using Sharlayan.Enums;

    internal class PartyMemberResolver {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MemoryHandler _memoryHandler;

        private MonsterWorkerDelegate _monsterWorkerDelegate;

        private NPCWorkerDelegate _npcWorkerDelegate;

        private PCWorkerDelegate _pcWorkerDelegate;

        private byte[] _statusesMap;

        private byte[] _statusMap;

        public PartyMemberResolver(MemoryHandler memoryHandler, PCWorkerDelegate pcWorkerDelegate, NPCWorkerDelegate npcWorkerDelegate, MonsterWorkerDelegate monsterWorkerDelegate) {
            this._memoryHandler = memoryHandler;
            this._pcWorkerDelegate = pcWorkerDelegate;
            this._npcWorkerDelegate = npcWorkerDelegate;
            this._monsterWorkerDelegate = monsterWorkerDelegate;
        }

        public PartyMember ResolvePartyMemberFromBytes(byte[] source, ActorItem actorItem = null) {
            if (actorItem != null) {
                PartyMember entry = new PartyMember {
                    X = actorItem.X,
                    Y = actorItem.Y,
                    Z = actorItem.Z,
                    Coordinate = actorItem.Coordinate,
                    ID = actorItem.ID,
                    UUID = actorItem.UUID,
                    Name = actorItem.Name,
                    Job = actorItem.Job,
                    Level = actorItem.Level,
                    HPCurrent = actorItem.HPCurrent,
                    HPMax = actorItem.HPMax,
                    MPCurrent = actorItem.MPCurrent,
                    HitBoxRadius = actorItem.HitBoxRadius,
                };
                entry.StatusItems.AddRange(actorItem.StatusItems);
                this.CleanXPValue(ref entry);
                return entry;
            }
            else {
                int defaultStatusEffectOffset = this._memoryHandler.Structures.PartyMember.DefaultStatusEffectOffset;
                PartyMember entry = new PartyMember();
                try {
                    const int limit = 15;
                    int statusSize = this._memoryHandler.Structures.StatusItem.SourceSize;
                    if (this._statusesMap == null) {
                        this._statusesMap = new byte[statusSize * 15];
                    }

                    if (this._statusMap == null) {
                        this._statusMap = new byte[statusSize];
                    }

                    entry.X = SharlayanBitConverter.TryToSingle(source, this._memoryHandler.Structures.PartyMember.X);
                    entry.Z = SharlayanBitConverter.TryToSingle(source, this._memoryHandler.Structures.PartyMember.Z);
                    entry.Y = SharlayanBitConverter.TryToSingle(source, this._memoryHandler.Structures.PartyMember.Y);
                    entry.Coordinate = new Coordinate(entry.X, entry.Z, entry.Z);
                    entry.ID = SharlayanBitConverter.TryToUInt32(source, this._memoryHandler.Structures.PartyMember.ID);
                    entry.UUID = Guid.NewGuid().ToString();
                    entry.Name = this._memoryHandler.GetStringFromBytes(source, this._memoryHandler.Structures.PartyMember.Name);
                    entry.JobID = source[this._memoryHandler.Structures.PartyMember.Job];
                    entry.Job = (Actor.Job) entry.JobID;
                    entry.HitBoxRadius = 0.5f;

                    entry.Level = source[this._memoryHandler.Structures.PartyMember.Level];
                    entry.HPCurrent = SharlayanBitConverter.TryToInt32(source, this._memoryHandler.Structures.PartyMember.HPCurrent);
                    entry.HPMax = SharlayanBitConverter.TryToInt32(source, this._memoryHandler.Structures.PartyMember.HPMax);
                    entry.MPCurrent = SharlayanBitConverter.TryToInt16(source, this._memoryHandler.Structures.PartyMember.MPCurrent);

                    List<StatusItem> foundStatuses = new List<StatusItem>();

                    Buffer.BlockCopy(source, defaultStatusEffectOffset, this._statusesMap, 0, limit * statusSize);
                    for (int i = 0; i < limit; i++) {
                        bool isNewStatus = false;

                        Buffer.BlockCopy(this._statusesMap, i * statusSize, this._statusMap, 0, statusSize);

                        short statusID = SharlayanBitConverter.TryToInt16(this._statusMap, this._memoryHandler.Structures.StatusItem.StatusID);
                        uint casterID = SharlayanBitConverter.TryToUInt32(this._statusMap, this._memoryHandler.Structures.StatusItem.CasterID);

                        StatusItem statusEntry = entry.StatusItems.FirstOrDefault(x => x.CasterID == casterID && x.StatusID == statusID);

                        if (statusEntry == null) {
                            statusEntry = new StatusItem();
                            isNewStatus = true;
                        }

                        statusEntry.TargetEntity = null;
                        statusEntry.TargetName = entry.Name;
                        statusEntry.StatusID = statusID;
                        statusEntry.Stacks = this._statusMap[this._memoryHandler.Structures.StatusItem.Stacks];
                        statusEntry.Duration = SharlayanBitConverter.TryToSingle(this._statusMap, this._memoryHandler.Structures.StatusItem.Duration);
                        statusEntry.CasterID = casterID;

                        foundStatuses.Add(statusEntry);

                        try {
                            ActorItem pc = this._pcWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            ActorItem npc = this._npcWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            ActorItem monster = this._monsterWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            statusEntry.SourceEntity = (pc ?? npc) ?? monster;
                        }
                        catch (Exception ex) {
                            this._memoryHandler.RaiseException(Logger, ex, true);
                        }

                        try {
                            if (statusEntry.StatusID > 0) {
                                Models.XIVDatabase.StatusItem statusInfo = StatusEffectLookup.GetStatusInfo((uint) statusEntry.StatusID);
                                statusEntry.IsCompanyAction = statusInfo.CompanyAction;
                                string statusKey = statusInfo.Name.English;
                                switch (this._memoryHandler.Configuration.GameLanguage) {
                                    case GameLanguage.French:
                                        statusKey = statusInfo.Name.French;
                                        break;
                                    case GameLanguage.Japanese:
                                        statusKey = statusInfo.Name.Japanese;
                                        break;
                                    case GameLanguage.German:
                                        statusKey = statusInfo.Name.German;
                                        break;
                                    case GameLanguage.Chinese:
                                        statusKey = statusInfo.Name.Chinese;
                                        break;
                                    case GameLanguage.Korean:
                                        statusKey = statusInfo.Name.Korean;
                                        break;
                                }

                                statusEntry.StatusName = statusKey;
                            }
                        }
                        catch (Exception) {
                            statusEntry.StatusName = "UNKNOWN";
                        }

                        if (statusEntry.IsValid()) {
                            if (isNewStatus) {
                                entry.StatusItems.Add(statusEntry);
                            }

                            foundStatuses.Add(statusEntry);
                        }
                    }

                    entry.StatusItems.RemoveAll(x => !foundStatuses.Contains(x));
                }
                catch (Exception ex) {
                    this._memoryHandler.RaiseException(Logger, ex, true);
                }

                this.CleanXPValue(ref entry);
                return entry;
            }
        }

        private void CleanXPValue(ref PartyMember entity) {
            if (entity.HPCurrent < 0 || entity.HPMax < 0) {
                entity.HPCurrent = 1;
                entity.HPMax = 1;
            }

            if (entity.HPCurrent > entity.HPMax) {
                if (entity.HPMax == 0) {
                    entity.HPCurrent = 1;
                    entity.HPMax = 1;
                }
                else {
                    entity.HPCurrent = entity.HPMax;
                }
            }

            if (entity.MPCurrent < 0 || entity.MPCurrent > 10000) {
                entity.MPCurrent = 10000;
            }
        }
    }
}