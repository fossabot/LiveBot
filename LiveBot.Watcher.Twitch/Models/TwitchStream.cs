﻿using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Helix.Models.Streams;

namespace LiveBot.Watcher.Twitch.Models
{
    /// <summary>
    /// Represents a Twitch Stream
    /// </summary>
    public class TwitchStream : BaseLiveBotStream
    {
        public TwitchStream(string baseURL, ServiceEnum serviceType, Stream stream, ILiveBotUser user, ILiveBotGame game) : base(baseURL, serviceType)
        {
            UserId = user.Id;
            User = user;
            Id = stream.Id;
            Title = stream.Title;
            StartTime = stream.StartedAt;
            GameId = game.Id;
            Game = game;
            ThumbnailURL = stream.ThumbnailUrl;
            StreamURL = $"{User.ProfileURL}";
        }

        public TwitchStream(string baseURL, ServiceEnum serviceType, Stream stream) : base(baseURL, serviceType)
        {
            UserId = stream.UserId;
            User = null;
            Id = stream.Id;
            Title = stream.Title;
            StartTime = stream.StartedAt;
            GameId = stream.GameId;
            Game = null;
            ThumbnailURL = stream.ThumbnailUrl;
            StreamURL = $"{baseURL}/{stream.UserName.Replace(" ", "")}";
        }
    }
}