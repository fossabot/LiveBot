﻿using Discord.WebSocket;
using LiveBot.API.Helpers;
using LiveBot.API.Models;
using LiveBot.API.Models.Discord;
using LiveBot.Core.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.API.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;

        public HomeController(ILogger<HomeController> logger, DiscordShardedClient client, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _client = client;
            _work = factory.Create();
        }

        public async Task<IActionResult> Index()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var guildsUserCanManage = await DiscordHelper.GetGuildsUserCanManage(HttpContext);
            }
            var subscriptions = await _work.SubscriptionRepository.GetAllAsync();
            var model = new DiscordStats
            {
                DiscordGuildCount = _client.Guilds.Count,
                SubscriptionCount = subscriptions.Count()
            };
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}