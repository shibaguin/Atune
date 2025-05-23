﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Atune.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MigrationHistoryTable", "__EFMigrationsHistory")
                .HasAnnotation("Relational:MigrationHistoryTableSchema", null);

            modelBuilder.Entity("Atune.Models.Album", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CoverArtPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Year")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Albums", (string)null);
                });

            modelBuilder.Entity("Atune.Models.AlbumArtist", b =>
                {
                    b.Property<int>("AlbumId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ArtistId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AlbumId", "ArtistId");

                    b.HasIndex("ArtistId");

                    b.ToTable("AlbumArtist");
                });

            modelBuilder.Entity("Atune.Models.Artist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Artists", (string)null);
                });

            modelBuilder.Entity("Atune.Models.MediaItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AlbumId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CoverArt")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("Duration")
                        .HasColumnType("BIGINT");

                    b.Property<string>("Genre")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("ReleaseDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<uint>("Year")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Path")
                        .IsUnique()
                        .HasDatabaseName("IX_MediaItems_Path");

                    b.HasIndex("AlbumId", "Title")
                        .HasDatabaseName("IX_MediaItems_Album_Title");

                    b.ToTable("MediaItems", (string)null);
                });

            modelBuilder.Entity("Atune.Models.PlayHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AppVersion")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("DeviceId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int>("DurationSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MediaItemId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OS")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<double>("PercentPlayed")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("PlayedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MediaItemId", "PlayedAt")
                        .HasDatabaseName("IX_PlayHistories_MediaItem_PlayedAt");

                    b.ToTable("PlayHistories", (string)null);
                });

            modelBuilder.Entity("Atune.Models.PlaybackQueueItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("MediaItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Position")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("MediaItemId");

                    b.ToTable("PlaybackQueueItems", (string)null);
                });

            modelBuilder.Entity("Atune.Models.Playlist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSmart")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Playlists", (string)null);
                });

            modelBuilder.Entity("Atune.Models.PlaylistMediaItem", b =>
                {
                    b.Property<int>("PlaylistId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MediaItemId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("Position")
                        .HasColumnType("INTEGER");

                    b.HasKey("PlaylistId", "MediaItemId");

                    b.HasIndex("MediaItemId");

                    b.ToTable("PlaylistMediaItem", (string)null);
                });

            modelBuilder.Entity("Atune.Models.TrackArtist", b =>
                {
                    b.Property<int>("MediaItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ArtistId")
                        .HasColumnType("INTEGER");

                    b.HasKey("MediaItemId", "ArtistId");

                    b.HasIndex("ArtistId");

                    b.ToTable("TrackArtist");
                });

            modelBuilder.Entity("Atune.Models.AlbumArtist", b =>
                {
                    b.HasOne("Atune.Models.Album", "Album")
                        .WithMany("AlbumArtists")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Atune.Models.Artist", "Artist")
                        .WithMany("AlbumArtists")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");

                    b.Navigation("Artist");
                });

            modelBuilder.Entity("Atune.Models.MediaItem", b =>
                {
                    b.HasOne("Atune.Models.Album", "Album")
                        .WithMany("Tracks")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");
                });

            modelBuilder.Entity("Atune.Models.PlayHistory", b =>
                {
                    b.HasOne("Atune.Models.MediaItem", "MediaItem")
                        .WithMany()
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MediaItem");
                });

            modelBuilder.Entity("Atune.Models.PlaybackQueueItem", b =>
                {
                    b.HasOne("Atune.Models.MediaItem", "MediaItem")
                        .WithMany()
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MediaItem");
                });

            modelBuilder.Entity("Atune.Models.PlaylistMediaItem", b =>
                {
                    b.HasOne("Atune.Models.MediaItem", "MediaItem")
                        .WithMany("PlaylistMediaItems")
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Atune.Models.Playlist", "Playlist")
                        .WithMany("PlaylistMediaItems")
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MediaItem");

                    b.Navigation("Playlist");
                });

            modelBuilder.Entity("Atune.Models.TrackArtist", b =>
                {
                    b.HasOne("Atune.Models.Artist", "Artist")
                        .WithMany("TrackArtists")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Atune.Models.MediaItem", "MediaItem")
                        .WithMany("TrackArtists")
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Artist");

                    b.Navigation("MediaItem");
                });

            modelBuilder.Entity("Atune.Models.Album", b =>
                {
                    b.Navigation("AlbumArtists");

                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("Atune.Models.Artist", b =>
                {
                    b.Navigation("AlbumArtists");

                    b.Navigation("TrackArtists");
                });

            modelBuilder.Entity("Atune.Models.MediaItem", b =>
                {
                    b.Navigation("PlaylistMediaItems");

                    b.Navigation("TrackArtists");
                });

            modelBuilder.Entity("Atune.Models.Playlist", b =>
                {
                    b.Navigation("PlaylistMediaItems");
                });
#pragma warning restore 612, 618
        }
    }
}
