﻿using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using MassTransit;
using MassTransit.Initializers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchStart : ILiveBotMonitorStart
    {
        /// <summary>
        /// Starts and runs the Twitch Monitoring Service
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public async Task StartAsync(IServiceProvider services)
        {
            TwitchMonitor service = (TwitchMonitor)services.GetRequiredService<List<ILiveBotMonitor>>().Where(i => i is TwitchMonitor).First();
            service.services = services;
            service._factory = services.GetRequiredService<IUnitOfWorkFactory>();
            service._bus = services.GetRequiredService<IBusControl>();

            service.ClientId = Environment.GetEnvironmentVariable("TwitchClientId");
            service.ClientSecret = Environment.GetEnvironmentVariable("TwitchClientSecret");

            var auth = await service._work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == service.ServiceType && i.Expired == false);
            service.AccessToken = auth.AccessToken;

            var streamUsers = await service._work.UserRepository.FindAsync(i => i.ServiceType == service.ServiceType);
            List<string> channelList = new List<string>(streamUsers.Select(i => i.SourceID).Distinct());
            service.Monitor.SetChannelsById(channelList);

            await Task.Run(service.Monitor.Start);
        }
    }
}