﻿using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Watcher.Twitch.Contracts;
using LiveBot.Watcher.Twitch.Models;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Streams;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchMonitor : BaseLiveBotMonitor
    {
        public LiveStreamMonitorService Monitor;
        public TwitchAPI API;
        public IServiceProvider services;
        public IBusControl _bus;
        private readonly int RetryDelay = 1000 * 5;

        /// <summary>
        /// Represents the whole Service for Twitch Monitoring
        /// </summary>
        public TwitchMonitor()
        {
            BaseURL = "https://twitch.tv";
            ServiceType = ServiceEnum.TWITCH;
            URLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

            API = new TwitchAPI();
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 60, maxStreamRequestCountPerRequest: 100);

            Monitor.OnServiceStarted += Monitor_OnServiceStarted;
            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
            Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;
        }

        #region Events

        public void Monitor_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            Log.Debug("Monitor service successfully connected to Twitch!");
        }

        public async void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _UpdateUser(stream.User);
            await _PublishStreamOnline(stream);
        }

        public async void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _PublishStreamOffline(stream);
        }

        public async void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _PublishStreamOffline(stream);
        }

        #endregion Events

        #region API Calls

        private async Task<Game> API_GetGame(string gameId)
        {
            try
            {
                List<string> gameIDs = new List<string> { gameId };
                GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
                return games.Games.FirstOrDefault(i => i.Id == gameId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetGame(gameId);
            }
        }

        private async Task<User> API_GetUserByLogin(string username)
        {
            try
            {
                List<string> usernameList = new List<string> { username };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Login == username);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserByLogin(username);
            }
        }

        private async Task<User> API_GetUserById(string userId)
        {
            try
            {
                List<string> userIdList = new List<string> { userId };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Id == userId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserById(userId);
            }
        }

        private async Task<GetUsersResponse> API_GetUsersById(List<string> userIdList)
        {
            try
            {
                GetUsersResponse apiUsers = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUsers;
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUsersById(userIdList);
            }
        }

        private async Task<User> API_GetUserByURL(string url)
        {
            try
            {
                string username = GetURLRegex(URLPattern).Match(url).Groups["username"].ToString();
                return await API_GetUserByLogin(username: username);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserByURL(url);
            }
        }

        private async Task<Stream> API_GetStream(string userId)
        {
            try
            {
                List<string> userIdList = new List<string> { userId };
                GetStreamsResponse streams = await API.Helix.Streams.GetStreamsAsync(userIds: userIdList);
                return streams.Streams.FirstOrDefault(i => i.UserId == userId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetStream(userId);
            }
        }

        #endregion API Calls

        #region Misc Functions

        public async Task _UpdateUser(ILiveBotUser user)
        {
            StreamUser existingUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.SourceID == user.Id);

            if (existingUser != null)
            {
                if (DateTime.UtcNow.Subtract(existingUser.TimeStamp).TotalHours > 1)
                {
                    User apiUser = await API_GetUserById(user.Id);
                    TwitchUser twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
                    StreamUser streamUser = new StreamUser()
                    {
                        ServiceType = ServiceType,
                        SourceID = twitchUser.Id,
                        Username = twitchUser.Username,
                        DisplayName = twitchUser.DisplayName,
                        AvatarURL = twitchUser.AvatarURL,
                        ProfileURL = twitchUser.ProfileURL
                    };
                    await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == ServiceType && i.SourceID == user.Id));
                }
            }
        }

        public async Task<ILiveBotStream> GetStream(Stream stream)
        {
            ILiveBotUser liveBotUser = await GetUser(userId: stream.UserId);
            ILiveBotGame liveBotGame = await GetGame(gameId: stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, liveBotUser, liveBotGame);
        }

        #endregion Misc Functions

        #region Messaging Implementation

        public async Task _PublishStreamOnline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOnline { Stream = stream });
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish StreamOnline:\n{e}");
            }
        }

        public async Task _PublishStreamUpdate(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamUpdate { Stream = stream });
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish StreamUpdate:\n{e}");
            }
        }

        public async Task _PublishStreamOffline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOffline { Stream = stream });
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish StreamOffline:\n{e}");
            }
        }

        #endregion Messaging Implementation

        #region Interface Requirements

        /// <inheritdoc/>
        public override ILiveBotMonitorStart GetStartClass()
        {
            return new TwitchStart();
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotGame> GetGame(string gameId)
        {
            Game game;

            StreamGame existingGame = await _work.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.SourceId == gameId);
            if (existingGame != null)
            {
                if (DateTime.UtcNow.Subtract(existingGame.TimeStamp).TotalHours > 1)
                {
                    game = await API_GetGame(gameId);
                    TwitchGame twitchGame = new TwitchGame(BaseURL, ServiceType, game);
                    StreamGame streamGame = new StreamGame
                    {
                        ServiceType = ServiceType,
                        SourceId = twitchGame.Id,
                        Name = twitchGame.Name,
                        ThumbnailURL = twitchGame.ThumbnailURL
                    };
                    await _work.GameRepository.AddOrUpdateAsync(streamGame, i => (i.ServiceType == ServiceType && i.SourceId == gameId));
                    return twitchGame;
                }
                else
                {
                    return new TwitchGame(BaseURL, ServiceType, existingGame);
                }
            }
            game = await API_GetGame(gameId);
            return new TwitchGame(BaseURL, ServiceType, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream> GetStream(ILiveBotUser user)
        {
            Stream stream = await API_GetStream(user.Id);
            if (stream == null)
                return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser> GetUser(string username = null, string userId = null, string profileURL = null)
        {
            User apiUser;
            StreamUser streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && (i.Username == username || i.SourceID == userId || i.ProfileURL == profileURL));
            if (streamUser != null)
            {
                return new TwitchUser(BaseURL, ServiceType, streamUser);
            }

            if (!string.IsNullOrEmpty(username))
            {
                apiUser = await API_GetUserByLogin(username: username);
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                apiUser = await API_GetUserById(userId: userId);
            }
            else if (!string.IsNullOrEmpty(profileURL))
            {
                apiUser = await API_GetUserByURL(url: profileURL);
            }
            else
            {
                return null;
            }

            TwitchUser twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
            await _UpdateUser(twitchUser);

            return twitchUser;
        }

        /// <inheritdoc/>
        public override bool AddChannel(ILiveBotUser user)
        {
            var channels = Monitor.ChannelsToMonitor;
            if (!channels.Contains(user.Id))
            {
                channels.Add(user.Id);
                Monitor.SetChannelsById(channels);
            }

            if (Monitor.ChannelsToMonitor.Contains(user.Id))
                return true;
            return false;
        }

        /// <inheritdoc/>
        public override bool RemoveChannel(ILiveBotUser user)
        {
            var channels = Monitor.ChannelsToMonitor;
            if (channels.Contains(user.Id))
            {
                channels.Remove(user.Id);
                Monitor.SetChannelsById(channels);
            }

            if (!Monitor.ChannelsToMonitor.Contains(user.Id))
                return true;
            return false;
        }

        #endregion Interface Requirements
    }
}