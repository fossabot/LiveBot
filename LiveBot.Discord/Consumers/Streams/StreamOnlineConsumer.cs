﻿using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Streams
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly List<ILiveBotMonitor> _monitors;

        public StreamOnlineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBusControl bus, List<ILiveBotMonitor> monitors)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _monitors = monitors;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == stream.ServiceType).FirstOrDefault();
            ILiveBotUser user = await monitor.GetUser(userId: stream.UserId);
            ILiveBotGame game = await monitor.GetGame(gameId: stream.GameId);

            if (monitor == null)
                return;

            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            var streamGame = new StreamGame
            {
                ServiceType = stream.ServiceType,
                SourceId = game.Id,
                Name = game.Name,
                ThumbnailURL = game.ThumbnailURL
            };
            await _work.GameRepository.AddOrUpdateAsync(streamGame, i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
            streamGame = await _work.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);

            if (streamSubscriptions.Count() == 0)
                return;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                var discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(i => i == streamSubscription.DiscordChannel);
                var discordRole = await _work.RoleRepository.SingleOrDefaultAsync(i => i == streamSubscription.DiscordRole);
                var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i == streamSubscription.DiscordGuild);

                Expression<Func<StreamNotification, bool>> notificationPredicate = (i =>
                    i.User_SourceID == streamSubscription.User.SourceID &&
                    i.Stream_SourceID == stream.Id &&
                    i.Stream_StartTime == stream.StartTime &&
                    i.DiscordGuild_DiscordId == streamSubscription.DiscordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == streamSubscription.DiscordChannel.DiscordId &&
                    i.Game_SourceID == game.Id
                );

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == streamSubscription.User.SourceID &&
                    i.DiscordGuild_DiscordId == streamSubscription.DiscordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == streamSubscription.DiscordChannel.DiscordId
                );

                var guild = _client.GetGuild(streamSubscription.DiscordGuild.DiscordId);
                var shard = _client.GetShardFor(guild);
                SocketTextChannel channel = null;
                int channelCheckCount = 0;

                while (channel == null)
                {
                    if (channelCheckCount >= 5)
                    {
                        var errorMessage = $"Unable to get a Discord Channel for {streamSubscription.DiscordChannel.DiscordId} after {channelCheckCount} attempts";
                        Log.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }
                    channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);
                    channelCheckCount += 1;
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Delay check for 5 seconds
                }

                string notificationMessage = NotificationHelpers.GetNotificationMessage(stream: stream, subscription: streamSubscription, user: user, game: game);
                Embed embed = NotificationHelpers.GetStreamEmbed(stream: stream, user: user, game: game);

                StreamNotification streamNotification = new StreamNotification
                {
                    ServiceType = stream.ServiceType,
                    Success = false,
                    Message = notificationMessage,

                    User_SourceID = streamUser.SourceID,
                    User_Username = streamUser.Username,
                    User_DisplayName = streamUser.DisplayName,
                    User_AvatarURL = streamUser.AvatarURL,
                    User_ProfileURL = streamUser.ProfileURL,

                    Stream_SourceID = stream.Id,
                    Stream_Title = stream.Title,
                    Stream_StartTime = stream.StartTime,
                    Stream_ThumbnailURL = stream.ThumbnailURL,
                    Stream_StreamURL = stream.StreamURL,

                    Game_SourceID = streamGame?.SourceId,
                    Game_Name = streamGame?.Name,
                    Game_ThumbnailURL = streamGame?.ThumbnailURL,

                    DiscordGuild_DiscordId = discordGuild.DiscordId,
                    DiscordGuild_Name = discordGuild.Name,

                    DiscordChannel_DiscordId = discordChannel.DiscordId,
                    DiscordChannel_Name = discordChannel.Name,

                    DiscordRole_DiscordId = discordRole == null ? 0 : discordRole.DiscordId,
                    DiscordRole_Name = discordRole?.Name
                };

                var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                previousNotifications = previousNotifications.Where(i =>
                    i.Stream_StartTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 // If within an hour of their last start time
                    && i.Success == true // Only pull Successful notifications
                );

                // If there is already 1 or more notifications that were successful in the past hour
                // mark this current one as a success
                if (previousNotifications.Count() > 0)
                    streamNotification.Success = true;

                await _work.NotificationRepository.AddOrUpdateAsync(streamNotification, notificationPredicate);
                streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                // If the current notification was marked as a success, end processing
                if (streamNotification.Success == true)
                    return;

                try
                {
                    var discordMessage = await channel.SendMessageAsync(text: notificationMessage, embed: embed);
                    streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                    streamNotification.Success = true;
                    await _work.NotificationRepository.UpdateAsync(streamNotification);
                }
                catch (Exception e)
                {
                    Log.Error($"Error sending notification for {streamNotification.Id} {streamNotification.ServiceType} {streamNotification.User_Username} {streamNotification.DiscordGuild_DiscordId} {streamNotification.DiscordChannel_DiscordId} {streamNotification.DiscordRole_DiscordId} {streamNotification.Message}\n{e}");
                }
            }
        }
    }
}