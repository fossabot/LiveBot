﻿using Discord.Commands;
using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [Group("test")]
    public class TestCommands : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public TestCommands(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        [Command("ping")]
        public async Task TestAsync()
        {
            var msg = $@"Hi, I am a bot: {Context.Client.CurrentUser.IsBot}";
            await ReplyAsync(msg);
        }

        [Command("retrieve")]
        public async Task RetrieveAsync()
        {
            IDiscordGuild DBGuild = _work.GuildRepository.GetGuildAsync(Context.Guild.Id);
            await ReplyAsync($"The following name was retrieved from the Database: {DBGuild.Name}");
        }
    }
}