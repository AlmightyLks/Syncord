﻿using EasyCommunication;
using EasyCommunication.Helper;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using System;
using EasyCommunication.Events.Host.EventArgs;
using EasyCommunication.SharedTypes;
using SyncordInfo.EventArgs;
using SyncordInfo.Communication;
using System.Collections.Generic;
using SyncordBot.Models;
using System.Diagnostics;
using System.ComponentModel;
using SyncordInfo.ServerStats;
using EasyCommunication.Connection;

namespace SyncordBot.SyncordCommunication
{
    public sealed class CommunicationHandler
    {
        private Bot _bot;
        private EasyHost _easyHost;
        private List<EmbedQueue> _embedQueues;
        private ILogger _logger;

        public CommunicationHandler(EasyHost easyHost, Bot bot, ILogger logger)
        {
            _bot = bot;
            _logger = logger;
            _easyHost = easyHost;
            _embedQueues = new List<EmbedQueue>();


            easyHost.EventHandler.ReceivedData += ReceivedDataFromSLServer;
            easyHost.EventHandler.ClientConnected += SLServerConnected;
            easyHost.EventHandler.ClientDisconnected += SLServerDisconnected;

            _easyHost.Open();
        }

        public async Task CreateChannelEmbedQueues()
        {
            foreach (DedicatedGuild dedicatedGuild in Bot.GuildConfig.Guilds)
            {
                if (dedicatedGuild == null)
                    continue;

                var guild = await _bot.Client.GetGuildAsync(dedicatedGuild.GuildID);

                if (guild == null)
                {
                    _logger.Warning($"No Guild found for Guild ID {dedicatedGuild.GuildID}");
                    continue;
                }

                foreach (var dedicatedChannel in dedicatedGuild.DedicatedChannels)
                {
                    try
                    {
                        var channel = guild.GetChannel(dedicatedChannel.Value);
                        if (channel == null)
                        {
                            _logger.Warning($"No Channel found for Channel ID {dedicatedChannel.Value} | Guild {dedicatedGuild.GuildID}");
                            continue;
                        }

                        EmbedQueue embedQueueElement = _embedQueues.Find(_ => _.DiscordChannel.Id == dedicatedChannel.Value);

                        //If no server has associated their sl ip with a this channel yet
                        if (embedQueueElement == null)
                        {
                            var embedQueue = new EmbedQueue(_logger)
                            {
                                DiscordChannel = channel
                            };

                            switch (dedicatedChannel.Key)
                            {
                                case EventTypes.PlayerJoin:
                                    {
                                        embedQueue.PlayerJoinedQueue = new Dictionary<string, Queue<PlayerJoinLeave>>()
                                        {
                                            { dedicatedGuild.SLFullAddress, new Queue<PlayerJoinLeave>() }
                                        };
                                    }
                                    break;
                                case EventTypes.PlayerLeave:
                                    {
                                        embedQueue.PlayerLeftQueue = new Dictionary<string, Queue<PlayerJoinLeave>>()
                                        {
                                            { dedicatedGuild.SLFullAddress, new Queue<PlayerJoinLeave>() }
                                        };
                                    }
                                    break;
                                case EventTypes.RoundSummary:
                                    {
                                        embedQueue.RoundEndQueue = new Dictionary<string, Queue<RoundEnd>>()
                                        {
                                            { dedicatedGuild.SLFullAddress, new Queue<RoundEnd>() }
                                        };
                                    }
                                    break;
                                case EventTypes.PlayerDeath:
                                    {
                                        embedQueue.PlayerDeathQueue = new Dictionary<string, Queue<PlayerDeath>>()
                                        {
                                            { dedicatedGuild.SLFullAddress, new Queue<PlayerDeath>() }
                                        };
                                    }
                                    break;
                                case EventTypes.PlayerBan:
                                    {
                                        embedQueue.PlayerBanQueue = new Dictionary<string, Queue<PlayerBan>>()
                                        {
                                            { dedicatedGuild.SLFullAddress, new Queue<PlayerBan>() }
                                        };
                                    }
                                    break;
                            }

                            _embedQueues.Add(embedQueue);
                        }
                        //If there's an embed queue for this Discord Channel, add new queue entry depending on the event type
                        else
                        {
                            switch (dedicatedChannel.Key)
                            {
                                case EventTypes.PlayerJoin:
                                    embedQueueElement.PlayerJoinedQueue.Add(dedicatedGuild.SLFullAddress, new Queue<PlayerJoinLeave>());
                                    break;
                                case EventTypes.PlayerLeave:
                                    embedQueueElement.PlayerLeftQueue.Add(dedicatedGuild.SLFullAddress, new Queue<PlayerJoinLeave>());
                                    break;
                                case EventTypes.RoundSummary:
                                    embedQueueElement.RoundEndQueue.Add(dedicatedGuild.SLFullAddress, new Queue<RoundEnd>());
                                    break;
                                case EventTypes.PlayerDeath:
                                    embedQueueElement.PlayerDeathQueue.Add(dedicatedGuild.SLFullAddress, new Queue<PlayerDeath>());
                                    break;
                                case EventTypes.PlayerBan:
                                    embedQueueElement.PlayerBanQueue.Add(dedicatedGuild.SLFullAddress, new Queue<PlayerBan>());
                                    break;
                            }
                        }
                        _logger.Information($"{dedicatedGuild.SLFullAddress} | Associated {dedicatedChannel.Key} with channel {channel.Name} ({channel.Id})");
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Exception in CreateChannelEmbedQueues:\n{e}");
                    }
                }
            }
        }

        private void ReceivedDataFromSLServer(ReceivedDataEventArgs ev)
        {
            switch (ev.Type)
            {
                case DataType.ProtoBuf:
                    {
                        if (!ev.Data.TryDeserializeProtoBuf(out DataBase dataBase))
                            return;

                        //  If Bot & SL Server are on the same machine, make the identifier / key the localhost variant
                        //  Why? 
                        // - The user shall only have to enter 127.0.0.1 in the config instead of the possibly complicated public IPv4
                        // - One less headache to worry about when you have the bot and the SL Server on the same machine
                        //   while also having a dynamic ip - You don't have to re-type the IP every changing interval
                        string ipAddress = dataBase.SameMachine ? $"127.0.0.1:{dataBase.SLFullAddress.Split(':')[1]}" : dataBase.SLFullAddress;

                        if (ev.Data.TryDeserializeProtoBuf(out Ping ping) && ping != null)
                        {
                            ping.Received = DateTime.Now;
                            _easyHost.QueueData(ping, ev.Sender, DataType.ProtoBuf);
                        }
                        else if (ev.Data.TryDeserializeProtoBuf(out PlayerJoinLeave joinLeave) && joinLeave != null)
                        {
                            if (joinLeave.Identifier == "join")
                            {
                                Debug.WriteLine($"{joinLeave.Player.Nickname} joined {joinLeave.SLFullAddress}!");

                                var embedQueueElement = _embedQueues.Find(_ => _.PlayerJoinedQueue.ContainsKey(ipAddress));
                                if (embedQueueElement == null)
                                {
                                    _logger.Warning($"ReceivedDataFromSLServer: Received join data from unconfigured SL Server: {dataBase.SLFullAddress}");
                                    return;
                                }
                                Queue<PlayerJoinLeave> joinQueue = embedQueueElement.PlayerJoinedQueue[ipAddress];
                                if (joinQueue == null)
                                {
                                    _logger.Warning($"ReceivedDataFromSLServer: A configured SL Server has a null-queue");
                                    return;
                                }
                                joinQueue.Enqueue(joinLeave);
                            }
                            else if (joinLeave.Identifier == "leave")
                            {
                                Debug.WriteLine($"{joinLeave.Player.Nickname} left {joinLeave.SLFullAddress}!");
                                var embedQueueElement = _embedQueues.Find(_ => _.PlayerLeftQueue.ContainsKey(ipAddress));
                                if (embedQueueElement == null)
                                {
                                    _logger.Warning($"ReceivedDataFromSLServer: Received leave data from unconfigured SL Server");
                                    return;
                                }
                                Queue<PlayerJoinLeave> joinQueue = embedQueueElement.PlayerLeftQueue[ipAddress];
                                if (joinQueue == null)
                                {
                                    _logger.Warning($"ReceivedDataFromSLServer: A configured SL Server has a null-queue");
                                    return;
                                }
                                joinQueue.Enqueue(joinLeave);
                            }
                        }
                        else if (ev.Data.TryDeserializeProtoBuf(out RoundEnd roundEnd) && roundEnd != null)
                        {
                            Debug.WriteLine($"Round ended for {roundEnd.SLFullAddress}!");

                            var embedQueueElement = _embedQueues.Find(_ => _.RoundEndQueue.ContainsKey(ipAddress));
                            if (embedQueueElement == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: Received round end data from unconfigured SL Server: {dataBase.SLFullAddress}");
                                return;
                            }

                            Queue<RoundEnd> roundEndQueue = embedQueueElement.RoundEndQueue[ipAddress];
                            if (roundEndQueue == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: A configured SL Server has a null-queue");
                                return;
                            }

                            roundEndQueue.Enqueue(roundEnd);
                        }
                        else if (ev.Data.TryDeserializeProtoBuf(out PlayerDeath playerDeath) && playerDeath != null)
                        {
                            Debug.WriteLine($"Player Death for {playerDeath.SLFullAddress}!");

                            var embedQueueElement = _embedQueues.Find(_ => _.PlayerDeathQueue.ContainsKey(ipAddress));
                            if (embedQueueElement == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: Received join data from unconfigured SL Server: {dataBase.SLFullAddress}");
                                return;
                            }

                            Queue<PlayerDeath> deathQueue = embedQueueElement.PlayerDeathQueue[ipAddress];
                            if (deathQueue == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: A configured SL Server has a null-queue");
                                return;
                            }

                            deathQueue.Enqueue(playerDeath);
                        }
                        else if (ev.Data.TryDeserializeProtoBuf(out PlayerBan playerBan) && playerBan != null)
                        {
                            Debug.WriteLine($"Player Death for {playerBan.SLFullAddress}!");

                            var embedQueueElement = _embedQueues.Find(_ => _.PlayerBanQueue.ContainsKey(ipAddress));
                            if (embedQueueElement == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: Received join data from unconfigured SL Server: {dataBase.SLFullAddress}");
                                return;
                            }

                            Queue<PlayerBan> banQueue = embedQueueElement.PlayerBanQueue[ipAddress];
                            if (banQueue == null)
                            {
                                _logger.Warning($"ReceivedDataFromSLServer: A configured SL Server has a null-queue");
                                return;
                            }

                            banQueue.Enqueue(playerBan);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void SLServerDisconnected(ClientDisconnectedEventArgs ev)
        {
            _logger.Warning("A client disconnected");
        }
        private void SLServerConnected(ClientConnectedEventArgs ev)
        {
            _logger.Information("A client connected");
        }
    }
}
