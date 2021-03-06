﻿#region Copyright (C) 2020 Team MediaPortal
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
using System.Linq;
using System.Web.Mvc;
using MPExtended.Services.Common.Interfaces;
using MPExtended.Applications.WebMediaPortal.Code;
using MPExtended.Applications.WebMediaPortal.Models;
using MPExtended.Services.MediaAccessService.Interfaces;

namespace MPExtended.Applications.WebMediaPortal.Controllers
{
  [ServiceAuthorize]
  public class PictureLibraryController : BaseController
  {
    //
    // GET: /PictureLibrary/
    public ActionResult Index(string id = null, string filter = null)
    {
      var model = new PictureFolderViewModel(id, filter);
      if (model.Folder == null)
      {
        return HttpNotFound();
      }
      return View(model);
    }

    public ActionResult Details(string picture)
    {
      var model = new PictureViewModel(picture);
      if (model.Picture == null)
        return HttpNotFound();
      return View(model);
    }

    [HttpGet]
    public ActionResult PictureInfo(string picture)
    {
      var model = new PictureViewModel(picture);
      if (model.Picture == null)
        return HttpNotFound();
      return Json(model, JsonRequestBehavior.AllowGet);
    }

    public ActionResult Video(string video)
    {
      var model = new VideoViewModel(video);
      if (model.Video == null)
        return HttpNotFound();
      return View(model);
    }

    [HttpGet]
    public ActionResult VideoInfo(string video)
    {
      var model = new VideoViewModel(video);
      if (model.Video == null)
        return HttpNotFound();
      return Json(model, JsonRequestBehavior.AllowGet);
    }

    public ActionResult Play(string video)
    {
      var model = new VideoViewModel(video);
      if (model.Video == null)
        return HttpNotFound();
      return View(model);
    }

    public ActionResult Image(string picture, int width = 0, int height = 0)
    {
      return Images.ReturnFromService(WebMediaType.Picture, picture, WebFileType.Cover, width, height, "Images/default/picture-cover.png");
    }

    public ActionResult MobileVideo(string video, int width = 0, int height = 0)
    {
      return Images.ReturnFromService(WebMediaType.MobileVideo, video, WebFileType.Cover, width, height, "Images/default/mobilevideo-cover.png");
    }

    public ActionResult Fanart(string picture, int width = 0, int height = 0, int num = -1)
    {
      return Images.ReturnFromService(WebMediaType.Picture, picture, WebFileType.Backdrop, width, height, "Images/default/picture-fanart.png", num);
    }
    
    public ActionResult FolderCover(string folder, int width = 0, int height = 0)
    {
      return Images.ReturnFromService(WebMediaType.PictureFolder, folder, WebFileType.Cover, width, height, "Images/default/picture-folder.png");
    }

    public ActionResult FolderFanart(string folder, int width = 0, int height = 0, int num = -1)
    {
      return Images.ReturnFromService(WebMediaType.PictureFolder, folder, WebFileType.Backdrop, width, height, "Images/default/picture-folder-fanart.png", num);
    }

  }
}
