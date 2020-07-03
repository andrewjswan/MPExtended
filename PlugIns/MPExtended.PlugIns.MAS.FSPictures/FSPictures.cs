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
using MPExtended.Libraries.Service;
using MPExtended.Services.MediaAccessService.Interfaces;

namespace MPExtended.PlugIns.MAS.FSPictures
{
    [Export(typeof(IPictureLibrary))]
    [ExportMetadata("Name", "FS Pictures")]
    [ExportMetadata("Id", 1)]
    public class FSPictures : PictureLibraryBase
    {
        private string root;

        [ImportingConstructor]
        public FSPictures(IPluginData data) : base(data)
        {
            root = data.GetConfiguration("FS Pictures")["root"];
            Supported = Directory.Exists(root);            
        }

        public override IEnumerable<WebCategory> GetAllPictureCategories()
        {
            var list = new List<WebCategory>();
            var rootInfo = new DirectoryInfo(root);
            list.Add(new WebCategory() { Title = rootInfo.Name, Id = PathToId(rootInfo.FullName) });
            foreach (var dir in rootInfo.EnumerateDirectories())
            {
                list.Add(new WebCategory() { Title = dir.Name, Id = PathToId(dir.FullName) });
            }
            return list;
        }

        protected override string PathToId(string fullpath)
        {
            string rootInfo = Path.GetFullPath(root);
            fullpath = Path.GetFullPath(fullpath);
            if (!fullpath.StartsWith(rootInfo))
            {
                Log.Error("Got path {0} that doesn't start with the root {1}", fullpath, rootInfo);
                return "";
            }

            if (fullpath == rootInfo)
            {
                return "_root";
            }

            string text = fullpath.Substring(rootInfo.Length + 1);
            byte[] toEncodeAsBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(toEncodeAsBytes);
        }

        protected override string IdToPath(string id)
        {
            if (id == "_root")
            {
                return root;
            }

            byte[] encodedDataAsBytes = Convert.FromBase64String(id);
            string path = Encoding.UTF8.GetString(encodedDataAsBytes);
            return Path.Combine(root, path);
        }

        protected override List<WebCategory> GetHistory(string fullpath)
        {
			List<WebCategory> history = new List<WebCategory>();
			if (string.IsNullOrEmpty(fullpath))
			{
            	return history;
			}

            DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(fullpath)));
            while (dir != null)
            {
                history.Add(new WebCategory() { Title = dir.Name, Id = PathToId(dir.FullName) });
                if (dir.FullName == root)
                {
                    break;
                }
                dir = dir.Parent;
            }
          	return history;
        }
    }
}