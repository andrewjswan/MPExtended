﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MPExtended.Services.Common.Interfaces
{
    [DataContract]
    public enum WebMediaType
    {
        [EnumMember]
        Movie = 0,
        [EnumMember]
        MusicTrack = 1,
        [EnumMember]
        Picture = 2,
        [EnumMember]
        TVEpisode = 3,
        [EnumMember]
        File = 4,
        [EnumMember]
        TVShow = 5,
        [EnumMember]
        TVSeason = 6,
        [EnumMember]
        MusicAlbum = 7,
        [EnumMember]
        MusicArtist = 8,
        [EnumMember]
        Folder = 9,
        [EnumMember]
        Drive = 10,
        [EnumMember]
        Playlist = 11,
        [EnumMember]
        TV = 12,
        [EnumMember]
        Recording = 13,
        [EnumMember]
        Radio = 14,
        [EnumMember]
        Url = 15,
        [EnumMember]
        Collection = 16,
        [EnumMember]
        PictureFolder = 17,
        [EnumMember]
        MobileVideo = 18,
        [EnumMember]
        MovieActor = 19,
        [EnumMember]
        TVShowActor = 20,
        [EnumMember]
        MovieGenre = 21,
        [EnumMember]
        TVShowGenre = 22
  }
}
