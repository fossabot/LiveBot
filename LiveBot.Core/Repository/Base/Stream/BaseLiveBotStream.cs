﻿using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;
using System;

namespace LiveBot.Core.Repository.Base.Stream
{
    public abstract class BaseLiveBotStream : BaseLiveBot, ILiveBotStream
    {
        public BaseLiveBotStream(string baseURL, ServiceEnum serviceType) : base(baseURL, serviceType)
        {
        }

        public ILiveBotUser User { get; set; }

        public ILiveBotGame Game { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime StartTime { get; set; }

        public string ThumbnailURL { get; set; }

        public abstract string GetStreamURL();

        public override string ToString()
        {
            return $@"{ServiceType.ToString()}: {Title}";
        }
    }
}