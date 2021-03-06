﻿#region Copyright (C) 2011-2013 MPExtended
// Copyright (C) 2011-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using MoreLinq;
using MPExtended.Applications.WebMediaPortal.Code;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Config;
using MPExtended.Services.MediaAccessService.Interfaces.Music;
using MPExtended.Services.StreamingService.Interfaces;
using MPExtended.Services.Common.Interfaces;

namespace MPExtended.Applications.WebMediaPortal.Models
{
    public class StreamTarget
    {
        public VideoPlayer Player { get; set; }
        public bool HasVideo { get; set; }
        public string Name { get; set; }

        public StreamTarget(VideoPlayer player, bool video, string name)
        {
            this.Player = player;
            this.HasVideo = video;
            this.Name = name;
        }

        public static List<StreamTarget> GetAudioTargets()
        {
            return new List<StreamTarget>() {
                new StreamTarget(VideoPlayer.VLC, false, "pc-vlc-audio"),
                new StreamTarget(VideoPlayer.FlashAudio, false, "pc-flash-audio")
            };
        }

        public static List<StreamTarget> GetVideoTargets()
        {
            return new List<StreamTarget>() {
                new StreamTarget(VideoPlayer.VLC, true, "pc-vlc-video"),
                new StreamTarget(VideoPlayer.FlashVideo, true, "pc-flash-video"),
                new StreamTarget(VideoPlayer.HLS, true, "mobile-hls-video"),
                new StreamTarget(VideoPlayer.HLS, true, "pc-hls-video"),
            };
        }

        public static List<StreamTarget> GetAllTargets()
        {
            return GetAudioTargets().Concat(GetVideoTargets()).ToList();
        }
    }

    public class ProfileModel
    {
        public static IEnumerable<WebTranscoderProfile> GetProfilesForTargets(IWebStreamingService service, IEnumerable<string> targets)
        {
            return targets.Count() > 2 ?
                service.GetTranscoderProfiles().Where(x => x.Targets.Intersect(targets).Any()) :
                targets.SelectMany(target => service.GetTranscoderProfilesForTarget(target)).DistinctBy(x => x.Name);
        }
    }

    public class PlayerViewModel
    {
        public IEnumerable<string> Transcoders { get; set; }
        public string Transcoder { get; set; }
        public WebTranscoderProfile TranscoderProfile { get; set; }
        public string ContinuationId { get; set; }
        public VideoPlayer Player { get; set; }
        public string PlayerViewName { get; set; }
        public WebResolution Size { get; set; }
        public string URL { get; set; }
        public WebMediaType MediaType { get; set; }
        public string MediaId { get; set; }

        public IEnumerable<SelectListItem> TranscoderSelectList
        {
            get
            {
                return Transcoders.Select(tc => new SelectListItem()
                    {
                        Text = tc,
                        Value = tc,
                        Selected = tc == Transcoder
                    });
            }
        }
    }

    public class AlbumPlayerViewModel : PlayerViewModel
    {
        public static bool EnableAlbumPlayerForUserAgent(string userAgent)
        {
            var defaultProfile = Configuration.StreamingPlatforms.GetDefaultProfileForUserAgent(StreamingProfileType.Audio, userAgent);
            var profile = Connections.Current.MASStreamControl.GetTranscoderProfileByName(defaultProfile);
            return Configuration.WebMediaPortal.EnableAlbumPlayer && 
                   StreamTarget.GetAudioTargets().Any(audioTarget => profile.Targets.Contains(audioTarget.Name));
        }

        public IEnumerable<WebMusicTrackDetailed> Tracks { get; set; }

        public AlbumPlayerViewModel()
        {
            MediaType = WebMediaType.MusicAlbum;
        }

        public string GetTranscoderForTrack(WebMusicTrackDetailed track)
        {
            if (track.Path.First().EndsWith(".mp3") && TranscoderProfile.MIME == "audio/mpeg" && false)
            {
                return "Direct";
            }
            else
            {
                return Transcoder;
            }
        }
    }

    public class ArtistTracksPlayerViewModel : PlayerViewModel
    {
        public static bool EnableAlbumPlayerForUserAgent(string userAgent)
        {
            var defaultProfile = Configuration.StreamingPlatforms.GetDefaultProfileForUserAgent(StreamingProfileType.Audio, userAgent);
            var profile = Connections.Current.MASStreamControl.GetTranscoderProfileByName(defaultProfile);
            return Configuration.WebMediaPortal.EnableAlbumPlayer && 
                   StreamTarget.GetAudioTargets().Any(audioTarget => profile.Targets.Contains(audioTarget.Name));
        }

        public IEnumerable<WebMusicTrackDetailed> Tracks { get; set; }

        public ArtistTracksPlayerViewModel()
        {
            MediaType = WebMediaType.MusicArtist;
        }

        public string GetTranscoderForTrack(WebMusicTrackDetailed track)
        {
            if (track.Path.First().EndsWith(".mp3") && TranscoderProfile.MIME == "audio/mpeg" && false)
            {
                return "Direct";
            }
            else
            {
                return Transcoder;
            }
        }
    }

    public enum VideoPlayer
    {
        FlashVideo,
        FlashAudio,
        VLC,
        HLS
    }
}