﻿using SyncordBot.Models;
using System.Collections.Generic;

namespace SyncordBot.Configs.BotConfigs
{
    public class DedicatedGuild
    {
        public ulong GuildID { get; set; } = 0;
        public string SLFullAddress { get; set; } = "";
        public Dictionary<EventTypes, ulong> DedicatedChannels { get; set; } = new Dictionary<EventTypes, ulong>();
    }
}
