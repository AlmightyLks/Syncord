﻿using Newtonsoft.Json;
using SyncordInfo.Communication;
using System.Collections.Generic;
using System.IO;

namespace SyncordBot.Configs
{
    public sealed class AliasConfig
    {
        public Dictionary<string, string> Aliases { get; init; }
        public AliasConfig()
        {
            Aliases = new Dictionary<string, string>();
        }
        public static AliasConfig Load()
        {
            AliasConfig result = new AliasConfig();
            string configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
            string configPath = Path.Combine(configDirectory, "Alias-Config.json");

            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            if (!File.Exists(configPath))
                File.WriteAllText(configPath, JsonConvert.SerializeObject(result));
            else
                result = JsonConvert.DeserializeObject<AliasConfig>(File.ReadAllText(configPath));

            return result;
        }
        public bool TryGetAlias(DataBase evArgs, out string value)
        {
            bool result = false;

            //Is the public IP:Port is known in the aliases
            if (Aliases.TryGetValue(evArgs.SLFullAddress, out value))
            {
                result = true;
            }
            //Is it on the same machine and is the 127.0.0.1:Port variant known
            else if (evArgs.SameMachine && Aliases.TryGetValue($"127.0.0.1:{evArgs.SLFullAddress.Split(':')[1]}", out value))
            {
                result = true;
            }

            return result;
        }
    }
}
