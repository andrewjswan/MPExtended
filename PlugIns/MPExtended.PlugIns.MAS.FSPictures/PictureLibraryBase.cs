﻿#region Copyright (C) 2011-2013 MPExtended, 2020 Team MediaPortal
// Copyright (C) 2011-2013 MPExtended Developers, http://www.mpextended.com/
// Copyright (C) 2020 Team MediaPortal, http://www.team-mediaportal.com/
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Util;
using MPExtended.Services.Common.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces.Picture;

namespace MPExtended.PlugIns.MAS.FSPictures
{
    public abstract class PictureLibraryBase : IPictureLibrary
    {
        protected IPluginData data;
        protected string[] Extensions { get; set; }
        protected string[] VideoExtensions { get; set; }

        public bool Supported { get; set; }

        public PictureLibraryBase(IPluginData data)
        {
            this.data = data;
            Extensions = new string[] { ".jpg", ".png", ".bmp" };
            VideoExtensions = new string[] { ".mp4", ".3gp", ".avi", ".mkv" };
            Supported = true;
        }

        public IEnumerable<WebPictureFolder> GetAllPictureFolders()
        {
            return GetAllPictureCategories().Select(x => new WebPictureFolder()
                   {
                       Id = x.Id,
                       Title = x.Title,
                       Description = x.Description,
                       Artwork = GetArtworkForFolder(IdToPath(x.Id)),
                       Categories = GetHistory(IdToPath(x.Id))
                   });
        }

        public IEnumerable<WebPictureFolder> GetSubFoldersById(string folderId)
        {
            return GetSubCategoriesById(folderId).Select(x => new WebPictureFolder()
                   {
                       Id = x.Id,
                       Title = x.Title,
                       Description = x.Description,
                       Artwork = GetArtworkForFolder(IdToPath(x.Id)),
                       Categories = GetHistory(IdToPath(x.Id))
                   });
        }

        public IEnumerable<WebCategory> GetSubCategoriesById(string categoryId)
        {
            var list = new List<WebCategory>();
            var root = new DirectoryInfo(IdToPath(categoryId));
            foreach (var folder in root.EnumerateDirectories())
            {
                list.Add(new WebCategory() { Title = folder.Name, Id = PathToId(folder.FullName) });
            }
            return list;
        }

        public virtual IEnumerable<WebPictureBasic> GetAllPicturesBasic()
        {
            return GetAllPictureCategories().Select(x => SearchPictures(IdToPath(x.Id), true, GetWebPictureBasic)).SelectMany(x => x);
        }

        public virtual IEnumerable<WebPictureDetailed> GetAllPicturesDetailed()
        {
            return GetAllPictureCategories().Select(x => SearchPictures(IdToPath(x.Id), true, GetWebPictureDetailed)).SelectMany(x => x);
        }

        public virtual IEnumerable<WebPictureBasic> GetAllMobileVideosBasic()
        {
            return GetAllPictureCategories().Select(x => SearchMobileVideos(IdToPath(x.Id), true, GetWebMobileVideoBasic)).SelectMany(x => x);
        }
        
        public WebPictureBasic GetPictureBasic(string pictureId)
        {
            return GetWebPictureBasic(IdToPath(pictureId));
        }

        public WebPictureDetailed GetPictureDetailed(string pictureId)
        {
            return GetWebPictureDetailed(IdToPath(pictureId));
        }

        public IEnumerable<WebPictureBasic> GetPicturesBasicByCategory(string id)
        {
            return SearchPictures(IdToPath(id), false, GetWebPictureBasic);
        }

        public IEnumerable<WebPictureDetailed> GetPicturesDetailedByCategory(string id)
        {
            return SearchPictures(IdToPath(id), false, GetWebPictureDetailed);
        }

        public WebFileInfo GetFileInfo(string path)
        {
            return new WebFileInfo(PathUtil.StripFileProtocolPrefix(path));
        }

        public Stream GetFile(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public IEnumerable<WebSearchResult> Search(string text)
        {
            return new List<WebSearchResult>();
        }
        
        public WebPictureFolder GetPictureFolderById(string id)
        {
            return GetWebPictureFolder(IdToPath(id));
        }

        public WebPictureBasic GetMobileVideoBasic(string videoId)
        {
            return GetWebMobileVideoBasic(IdToPath(videoId));
        }
        
        public IEnumerable<WebPictureBasic> GetMobileVideosBasicByCategory(string id)
        {
            return SearchMobileVideos(IdToPath(id), false, GetWebMobileVideoBasic);
        }
        
        public WebDictionary<string> GetExternalMediaInfo(WebMediaType type, string id)
        {
            if (type == WebMediaType.PictureFolder)
            {
                return new WebDictionary<string>()
                {
                    { "Type", "folder" },
                    { "Id", GetPictureFolderById(id).Id }
                };
            }
            
            if (type == WebMediaType.MobileVideo)
            {
                return new WebDictionary<string>()
                {
                    { "Type", "mobile video" },
                    { "Id", GetMobileVideoBasic(id).Id }
                };
            }
            
            return new WebDictionary<string>()
            {
                { "Type", "file" },
                { "Path", GetPictureBasic(id).Path.First() },
            };
        }

        private delegate T CreatePicture<T>(string path);
        private List<T> SearchPictures<T>(string strDir, bool recursive, CreatePicture<T> creator)
        {
            try
            {
                List<T> output = new List<T>();
                foreach (string strFile in Directory.GetFiles(strDir))
                {
                    var file = new FileInfo(PathUtil.StripFileProtocolPrefix(strFile));
                    if (Extensions.Contains(file.Extension.ToLowerInvariant()))
                    {
                        output.Add(creator.Invoke(file.FullName));
                    }
                }

                if (recursive)
                {
                    foreach (string strDirectory in Directory.GetDirectories(strDir))
                    {
                        output.AddRange(SearchPictures(strDirectory, true, creator));
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed in recursive picture search", ex);
                return new List<T>();
            }
        }

        private WebPictureBasic GetWebPictureBasic(string path)
        {
            WebPictureBasic pic = new WebPictureBasic();
            pic.Id = PathToId(path);
            pic.Path.Add(path);
            // pic.Categories.Add(GetCategoryFromPath(path));
            pic.Categories = GetHistory(path);

            // Image metadata
            Uri uri = new Uri(path);
            if (!PathUtil.MightBeOnNetworkDrive(uri))
            {
                try
                {
                    BitmapSource img = BitmapFrame.Create(uri);
                    BitmapMetadata meta = (BitmapMetadata)img.Metadata;

                    if (!String.IsNullOrWhiteSpace(meta.Title))
                        pic.Title = meta.Title.Trim();
                    if (!String.IsNullOrWhiteSpace(meta.DateTaken))
                        pic.DateTaken = DateTime.Parse(meta.DateTaken);
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Error reading picture (meta-)data for {0}", path), ex);
                }
            }

            // Set title to file name if non-existant
            if (String.IsNullOrEmpty(pic.Title))
                pic.Title = Path.GetFileName(path);

            pic.Artwork = GetArtworkForPicture(path);

            return pic;
        }

        private WebPictureDetailed GetWebPictureDetailed(string path)
        {
            WebPictureDetailed pic = new WebPictureDetailed();
            pic.Id = PathToId(path);
            pic.Path.Add(path);
            // pic.Categories.Add(GetCategoryFromPath(path));
            pic.Categories = GetHistory(path);

            // Image data
            Uri uri = new Uri(path);
            try
            {
                BitmapSource img = BitmapFrame.Create(uri);
                pic.Mpixel = (img.PixelHeight * img.PixelWidth * 1.0) / (1000 * 1000);
                pic.Width = Convert.ToString(img.PixelWidth);
                pic.Height = Convert.ToString(img.PixelHeight);
                pic.Dpi = Convert.ToString(img.DpiX * img.DpiY);

                // Image metadata
                BitmapMetadata meta = (BitmapMetadata)img.Metadata;
                if (!String.IsNullOrWhiteSpace(meta.Title))
                    pic.Title = meta.Title.Trim();
                if (!String.IsNullOrWhiteSpace(meta.DateTaken))
                    pic.DateTaken = DateTime.Parse(meta.DateTaken);
                pic.Comment = meta.Comment;
                pic.CameraManufacturer = meta.CameraManufacturer;
                pic.CameraModel = meta.CameraModel;
                pic.Copyright = meta.Copyright;
                pic.Rating = (float)meta.Rating;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Error reading picture (meta-)data for {0}", path), ex);
            }
            
            // Set title to file name if non-existant
            if (String.IsNullOrEmpty(pic.Title))
                pic.Title = Path.GetFileName(path);

            pic.Artwork = GetArtworkForPicture(path);

            return pic;
        }

        private delegate T CreateMobileVideo<T>(string path);
        private List<T> SearchMobileVideos<T>(string strDir, bool recursive, CreateMobileVideo<T> creator)
        {
            try
            {
                List<T> output = new List<T>();
                foreach (string strFile in Directory.GetFiles(strDir))
                {
                    var file = new FileInfo(PathUtil.StripFileProtocolPrefix(strFile));
                    if (VideoExtensions.Contains(file.Extension.ToLowerInvariant()))
                    {
                        output.Add(creator.Invoke(file.FullName));
                    }
                }

                if (recursive)
                {
                    foreach (string strDirectory in Directory.GetDirectories(strDir))
                    {
                        output.AddRange(SearchPictures(strDirectory, true, creator));
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed in recursive mobile video search", ex);
                return new List<T>();
            }
        }

        private WebPictureBasic GetWebMobileVideoBasic(string path)
        {
            WebPictureBasic vid = new WebMobileVideoBasic();
            vid.Id = PathToId(path);
            vid.Path.Add(path);
            vid.Categories = GetHistory(path);

            // MobileVideo metadata
            // TODO Add ...

            // Set title to file name if non-existant
            if (String.IsNullOrEmpty(vid.Title))
                vid.Title = Path.GetFileName(path);

            vid.Artwork = GetArtworkForMobileVideo(path);

            return vid;
        }

        #region Artworks

        private List<WebArtwork> GetArtworkForPicture(string picture)
        {
            var artwork = new List<WebArtwork>();
            if (string.IsNullOrEmpty(picture) || !File.Exists(picture))
                return artwork;

            artwork.Add(new WebArtworkDetailed()
            {
              Type = WebFileType.Cover,
              Offset = 0,
              Path = picture,
              Rating = 1,
              Id = picture.GetHashCode().ToString(),
              Filetype = Path.GetExtension(picture).Substring(1)
            });
            artwork.Add(new WebArtworkDetailed()
            {
              Type = WebFileType.Poster,
              Offset = 0,
              Path = picture,
              Rating = 1,
              Id = picture.GetHashCode().ToString(),
              Filetype = Path.GetExtension(picture).Substring(1)
            });
            artwork.Add(new WebArtworkDetailed()
            {
              Type = WebFileType.Backdrop,
              Offset = 0,
              Path = picture,
              Rating = 1,
              Id = picture.GetHashCode().ToString(),
              Filetype = Path.GetExtension(picture).Substring(1)
            });

            return artwork;
        }
        
        private List<WebArtwork> GetArtworkForMobileVideo(string picture)
        {
            var artwork = new List<WebArtwork>();
            if (string.IsNullOrEmpty(picture) || !File.Exists(picture))
                return artwork;
                
            // TODO
            
            return artwork;
        }

        private List<WebArtwork> GetArtworkForFolder(string path)
        {
            var artwork = new List<WebArtwork>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return artwork;

            // Poster
            int i = 0;
            string folder = Path.Combine(path, "folder{0}");
            var files = Extensions.Select(x => string.Format(folder, x))
                                  .Where(x => File.Exists(x))
                                  .Distinct();
            foreach (string file in files)
            {
                artwork.Add(new WebArtworkDetailed()
                {
                    Type = WebFileType.Cover,
                    Offset = i++,
                    Path = file,
                    Rating = 1 + i,
                    Id = file.GetHashCode().ToString(),
                    Filetype = Path.GetExtension(file).Substring(1)
                });
            }

            return artwork;
        }

        #endregion

        private WebPictureFolder GetWebPictureFolder(string path)
        {
            WebCategory category = GetCategoryFromPath(path);
            return new WebPictureFolder()
            {
                Id = category.Id,
                Title = category.Title,
                Description = category.Description,
                Artwork = GetArtworkForFolder(path),
                Categories = GetHistory(path)
            };
        }
        
        private WebCategory GetCategoryFromPath(string path)
        {
            string dir = Path.GetDirectoryName(path);

            return new WebCategory()
            {
                Title = Path.GetFileName(dir),
                Id = PathToId(dir)
            };
        }

        public abstract IEnumerable<WebCategory> GetAllPictureCategories();
        protected abstract string PathToId(string fullpath);
        protected abstract string IdToPath(string id);
        protected abstract List<WebCategory> GetHistory(string fullpath);
    }
}
