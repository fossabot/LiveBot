﻿// <auto-generated />
using System;
using LiveBot.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LiveBot.Repository.Migrations
{
    [DbContext(typeof(LiveBotDBContext))]
    partial class LiveBotDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("DiscordGuildId")
                        .HasColumnType("integer");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId");

                    b.ToTable("DiscordChannel");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordGuild", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.ToTable("DiscordGuild");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("DiscordGuildId")
                        .HasColumnType("integer");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId");

                    b.ToTable("DiscordRole");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Streams.StreamSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<int?>("DiscordChannelId")
                        .HasColumnType("integer");

                    b.Property<int?>("DiscordGuildId")
                        .HasColumnType("integer");

                    b.Property<int?>("DiscordRoleId")
                        .HasColumnType("integer");

                    b.Property<string>("Message")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("DiscordChannelId");

                    b.HasIndex("DiscordGuildId");

                    b.HasIndex("DiscordRoleId");

                    b.HasIndex("UserId");

                    b.ToTable("StreamSubscription");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Streams.StreamUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("AvatarURL")
                        .HasColumnType("text");

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("ProfileURL")
                        .HasColumnType("text");

                    b.Property<int>("ServiceType")
                        .HasColumnType("integer");

                    b.Property<string>("SourceID")
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("StreamUser");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordChannel", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", "DiscordGuild")
                        .WithMany("DiscordChannels")
                        .HasForeignKey("DiscordGuildId");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Discord.DiscordRole", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", "DiscordGuild")
                        .WithMany("DiscordRoles")
                        .HasForeignKey("DiscordGuildId");
                });

            modelBuilder.Entity("LiveBot.Core.Repository.Models.Streams.StreamSubscription", b =>
                {
                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordChannel", "DiscordChannel")
                        .WithMany("StreamSubscriptions")
                        .HasForeignKey("DiscordChannelId");

                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordGuild", null)
                        .WithMany("StreamSubscriptions")
                        .HasForeignKey("DiscordGuildId");

                    b.HasOne("LiveBot.Core.Repository.Models.Discord.DiscordRole", "DiscordRole")
                        .WithMany()
                        .HasForeignKey("DiscordRoleId");

                    b.HasOne("LiveBot.Core.Repository.Models.Streams.StreamUser", "User")
                        .WithMany("StreamSubscriptions")
                        .HasForeignKey("UserId");
                });
#pragma warning restore 612, 618
        }
    }
}
