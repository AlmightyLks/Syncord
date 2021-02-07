﻿/*
    The following license applies to the entirety of this Repository and Solution.
    
    TLDR.: Don't use a damn thing from my work without crediting me, else I'll smite your arse.
    
    Copyright 2021 AlmightyLks

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the License for the specific language governing
    permissions and limitations under the License.
*/
using DSharpPlus.Entities;
using SyncordBot.Models;
using SyncordInfo.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncordBot.SyncordCommunication
{
    public static class SyncordEmbedBuilder
    {
        public static DiscordEmbed ToEmbed(this PlayerJoinLeave[] ev)
        {
            Translation translation = Bot.Configs.TranslationConfig.Translation;
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Title = ev[0].Identifier == "join" ? translation.Elements["Player Join"] : translation.Elements["Player Leave"];
            embedBuilder.Color = ev[0].Identifier == "join" ? DiscordColor.Green : DiscordColor.IndianRed;
            foreach (var joinedArgs in ev)
            {
                var strBuilder = new StringBuilder();
                strBuilder.AppendLine("```");

                if (Bot.Configs.EmbedConfigs.PlayerJoinedLeftConfig.ShowUserId)
                    strBuilder.AppendLine(joinedArgs.Player.UserId);
                if (Bot.Configs.EmbedConfigs.PlayerJoinedLeftConfig.ShowPing)
                    strBuilder.AppendLine($"{joinedArgs.Player.Ping} {translation.Elements["ms"]}");
                if (Bot.Configs.EmbedConfigs.PlayerJoinedLeftConfig.ShowIP)
                    strBuilder.AppendLine($"{(joinedArgs.Player.DoNotTrack ? "Do Not Track" : joinedArgs.Player.IPAddress)}");

                strBuilder.AppendLine(joinedArgs.Time.ToLongTimeString());
                strBuilder.AppendLine("```");

                embedBuilder.AddField(string.IsNullOrWhiteSpace(joinedArgs.Player.Nickname) ? "[Empty-Name]" : joinedArgs.Player.Nickname,
                    strBuilder.ToString(),
                    true);

                embedBuilder.WithFooter($"{translation.Elements["Server"]}: {joinedArgs.SLFullAddress}");
            }

            return embedBuilder.Build();
        }

        public static DiscordEmbed ToEmbed(this PlayerDeath[] ev)
        {
            Translation translation = Bot.Configs.TranslationConfig.Translation;
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Title = translation.Elements["Player Death"];
            embedBuilder.Color = DiscordColor.Red;
            foreach (var playerDeath in ev)
            {
                var damageType = playerDeath.HitInfo.DamageType;

                string victimRole = playerDeath.Victim.Role.Role.Name;
                string killerRole = playerDeath.Killer.Role.Role.Name;

                if (Bot.Configs.EmbedConfigs.PlayerDeathConfig.ShowUserId)
                {
                    embedBuilder.AddField($"{translation.Elements["Killer"]}",
                        $"{killerRole}\n{playerDeath.Killer.DisplayName}\n{playerDeath.Killer.UserId}",
                        true);
                    embedBuilder.AddField($"{translation.Elements["Victim"]}",
                        $"{victimRole}\n{playerDeath.Victim.DisplayName}\n{playerDeath.Victim.UserId}\n{(playerDeath.Victim.IsCuffed ? translation.Elements["Was Cuffed"] : translation.Elements["Was not Cuffed"])}"
                        , true);
                }
                else
                {
                    embedBuilder.AddField($"{translation.Elements["Killer"]}",
                        $"{killerRole}\n{playerDeath.Killer.DisplayName}",
                        true);
                    embedBuilder.AddField($"{translation.Elements["Victim"]}",
                        $"{victimRole}\n{playerDeath.Victim.DisplayName}\n{(playerDeath.Victim.IsCuffed ? translation.Elements["Was Cuffed"] : translation.Elements["Was not Cuffed"])}"
                        , true);
                }
                embedBuilder.AddField($"{translation.Elements["Weapon"]}:",
                    $"{damageType.Name}",
                    true);

                embedBuilder.WithFooter($"{translation.Elements["Server"]}: {playerDeath.SLFullAddress}");
                embedBuilder.Timestamp = playerDeath.Time;
            }

            return embedBuilder.Build();
        }

        public static DiscordEmbed ToEmbed(this RoundEnd ev)
        {
            Translation translation = Bot.Configs.TranslationConfig.Translation;
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Title = translation.Elements["Round Summary"];
            embedBuilder.Color = DiscordColor.Gold;

            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowRoundLength)
                embedBuilder.AddField($"{translation.Elements["Round Length"]}",
                    TimeSpan.FromSeconds(ev.RoundSummary.RoundTime).ToString(),
                    false);
            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowTotalKills)
                embedBuilder.AddField($"{translation.Elements["Total Kills"]}",
                ev.RoundSummary.TotalKills.ToString(),
                true);
            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowTotalScpKills)
                embedBuilder.AddField($"{translation.Elements["Kills By SCPs"]}",
                ev.RoundSummary.TotalKillsByScps.ToString(),
                true);
            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowTotalFragGrenadeKills)
                embedBuilder.AddField($"{translation.Elements["Kills By Frag Grenades"]}",
                ev.RoundSummary.TotalKillsByFragGrenade.ToString(),
                true);

            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowTotalEscapedDClass)
                embedBuilder.AddField($"{translation.Elements["Escaped D-Class"]}",
                ev.RoundSummary.TotalEscapedDClass.ToString(),
                true);
            if (Bot.Configs.EmbedConfigs.RoundEndConfig.ShowTotalEscapedScientists)
                embedBuilder.AddField($"{translation.Elements["Escaped Scientists"]}",
                ev.RoundSummary.TotalEscapedScientists.ToString(),
                true);

            embedBuilder.WithFooter($"{translation.Elements["Server"]}: {ev.SLFullAddress}");
            embedBuilder.Timestamp = ev.Time;

            return embedBuilder.Build();
        }

        public static DiscordEmbed ToEmbed(this PlayerBan[] ev)
        {
            Translation translation = Bot.Configs.TranslationConfig.Translation;
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Title = translation.Elements["Player Ban"];
            embedBuilder.Color = DiscordColor.DarkRed;
            foreach (var playerBan in ev)
            {
                embedBuilder.AddField($"{playerBan.BanningPlayer.Nickname} ({playerBan.BanningPlayer.UserId})",
                    $"{translation.Elements["Banned"]}: {playerBan.BannedPlayer.Nickname}\n{playerBan.BannedPlayer.UserId}\n{translation.Elements["Reason"]}: {playerBan.Reason}\n" +
                    $"{translation.Elements["Duration"]}: {playerBan.Duration / 60} {translation.Elements["Minutes"]} | " +
                    $"{playerBan.Duration / 60 / 60} {translation.Elements["Hours"]} | " +
                    $"{playerBan.Duration / 60 / 60 / 24} {translation.Elements["Days"]} | " +
                    $"{playerBan.Duration / 60 / 60 / 24 / 365} {translation.Elements["Years"]}",
                    true);

                embedBuilder.WithFooter($"{translation.Elements["Server"]}: {playerBan.SLFullAddress}");
                embedBuilder.Timestamp = playerBan.Time;
            }


            return embedBuilder.Build();
        }
    }
}
