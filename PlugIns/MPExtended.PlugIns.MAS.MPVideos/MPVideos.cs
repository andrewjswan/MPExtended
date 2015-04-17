#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
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
using System.Globalization;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Util;
using MPExtended.Services.Common.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces.Movie;
using MPExtended.Libraries.SQLitePlugin;

namespace MPExtended.PlugIns.MAS.MPVideos
{
    [Export(typeof(IMovieLibrary))]
    [ExportMetadata("Name", "MP MyVideo")]
    [ExportMetadata("Id", 7)]
    public class MPVideos : Database, IMovieLibrary
    {
        public bool Supported { get; set; }
        private videodatabaseEntities _connection;

        [ImportingConstructor]
        public MPVideos(IPluginData data)
        {
          ConnectDb();
        }

        private void ConnectDb()
        {
          try
          {
            string ConnectionString = string.Format(
              "metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=MySql.Data.MySqlClient;provider connection string=\"server={0};user id={1};password={2};persistsecurityinfo=True;database=videodatabase;Convert Zero Datetime=True;charset=utf8\"",
              "localhost", "root", "MediaPortal");

            _connection = new videodatabaseEntities(ConnectionString);

            Supported = true;
          }
          catch (Exception ex)
          {
            Log.Error("Videodatabase:ConnectDb exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
            Supported = false;
          }
        }

        private List<WebActor> ActorReader(SQLiteDataReader reader, int idx)
        {
            return ((IList<string>)DataReaders.ReadPipeList(reader, idx)).Select(x => new WebActor() { Title = x }).ToList();
        }

        private List<string> CreditsReader(SQLiteDataReader reader, int idx)
        {
            return ((string)DataReaders.ReadString(reader, idx))
                .Split('/')
                .Select(x => x.Trim())
                .ToList();
        }

        private LazyQuery<T> LoadMovies<T>() where T : WebMovieBasic, new()
        {
            string mp13Fields = VersionUtil.GetMediaPortalVersion() >= VersionUtil.MediaPortalVersion.MP1_3 ? "i.strDirector, i.dateAdded, " : String.Empty;
            string sql =
                "SELECT m.idMovie, i.strTitle, i.iYear, i.fRating, i.runtime, i.IMDBID, i.strPlot, i.strPictureURL, i.strCredits, i.iswatched, " + mp13Fields +
                    "GROUP_CONCAT(p.strPath || f.strFilename, '|') AS fullpath, " +
                    "GROUP_CONCAT(a.strActor, '|') AS actors, " +
                    "GROUP_CONCAT(g.strGenre, '|') AS genres " +
                "FROM movie m " +
                "INNER JOIN movieinfo i ON m.idMovie = i.idMovie " +
                "LEFT JOIN files f ON m.idMovie = f.idMovie " +
                "LEFT JOIN path p ON f.idPath = p.idPath " +
                "LEFT JOIN actorlinkmovie alm ON m.idMovie = alm.idMovie " +
                "LEFT JOIN actors a ON alm.idActor = a.idActor " +
                "LEFT JOIN genrelinkmovie glm ON m.idMovie = glm.idMovie " +
                "LEFT JOIN genre g ON glm.idGenre = g.idGenre " +
                "WHERE %where " +
                "GROUP BY m.idMovie, i.strTitle, i.iYear, i.fRating, i.runtime, i.IMDBID, i.strPlot, i.strPictureURL";
            return new LazyQuery<T>(this, sql, new List<SQLFieldMapping>()
            {
                new SQLFieldMapping("m", "idMovie", "Id", DataReaders.ReadIntAsString),
                new SQLFieldMapping("fullpath", "Path", DataReaders.ReadPipeList),
                new SQLFieldMapping("actors", "Actors", ActorReader),
                new SQLFieldMapping("genres", "Genres", DataReaders.ReadPipeList),
                new SQLFieldMapping("i", "strPictureURL", "Artwork", ArtworkRetriever.ArtworkReader),
                new SQLFieldMapping("i", "strTitle", "Title", DataReaders.ReadString),
                new SQLFieldMapping("i", "iYear", "Year", DataReaders.ReadInt32),
                new SQLFieldMapping("i", "fRating", "Rating", DataReaders.ReadStringAsFloat),
                new SQLFieldMapping("i", "runtime", "Runtime", DataReaders.ReadInt32),
                new SQLFieldMapping("i", "IMDBID", "IMDBId", DataReaders.ReadString),
                new SQLFieldMapping("i", "strPlot", "Summary", DataReaders.ReadString),
                new SQLFieldMapping("i", "strCredits", "Writers", CreditsReader),
                new SQLFieldMapping("i", "iswatched", "Watched", DataReaders.ReadBoolean),
                new SQLFieldMapping("i", "strDirector", "Directors", DataReaders.ReadStringAsList),
                new SQLFieldMapping("i", "dateAdded", "DateAdded", DataReaders.ReadDateTime)
            });
        }

        public IEnumerable<WebMovieBasic> GetAllMovies()
        {
          string sql = "SELECT * FROM movieinfo";

          var query = _connection.ExecuteStoreQuery<movieinfo>(sql).ToList();
          var result = new List<WebMovieBasic>();

          foreach (movieinfo item in query)
          {
            WebMovieBasic a = new WebMovieBasic();
            a.Id = item.idMovie.ToString();

            sql = "SELECT CONCAT(p.strPath, f.strFilename) AS fullpath FROM files f, path p where p.idPath = f.idPath and f.idMovie = " + item.idMovie.ToString();
            var query2 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            a.Path = query2;

            sql = "SELECT strActor FROM actorlinkmovie alm, actors a where alm.idActor = a.idActor and alm.idMovie =" + item.idMovie.ToString();
            var query3 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            var actr = new List<WebActor>();

            foreach (WebActor item2 in query3)
            {
              WebActor a2 = new WebActor();
              a2.Title = item.strGenre;
              actr.Add(a2);
            }
            a.Actors = actr;

            sql = "SELECT strGenre FROM genrelinkmovie glm, genre g where glm.idGenre = g.idGenre and glm.idMovie =" + item.idMovie.ToString();
            var query4 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            a.Genres = query4;

            a.Title = item.strTitle;
            a.DateAdded = item.dateAdded;
            result.Add(a);
          }
          return result;
        }

        public IEnumerable<WebMovieDetailed> GetAllMoviesDetailed()
        {
          string sql = "SELECT * FROM movieinfo";

          var query = _connection.ExecuteStoreQuery<movieinfo>(sql).ToList();
          var result = new List<WebMovieDetailed>();

          foreach (movieinfo item in query)
          {
            WebMovieDetailed a = new WebMovieDetailed();
            a.Id = item.idMovie.ToString();

            sql = "SELECT CONCAT(p.strPath, f.strFilename) AS fullpath FROM files f, path p where p.idPath = f.idPath and f.idMovie = " + item.idMovie.ToString();
            var query2 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            a.Path = query2;
            a.Title = item.strTitle;
            a.Year = (int)item.iYear;
            a.Runtime = (int)item.runtime;
            a.Summary = item.strPlot;
            a.Watched = Convert.ToBoolean(item.iswatched);

            sql = "SELECT strActor FROM actorlinkmovie alm, actors a where alm.idActor = a.idActor and alm.idMovie =" + item.idMovie.ToString();
            var query3 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            var actr = new List<WebActor>();

            foreach (WebActor item2 in query3)
            {
              WebActor a2 = new WebActor();
              a2.Title = item2;
              actr.Add(a2);
            }
            a.Actors = actr;

            sql = "SELECT strGenre FROM genrelinkmovie glm, genre g where glm.idGenre = g.idGenre and glm.idMovie =" + item.idMovie.ToString();
            var query4 = _connection.ExecuteStoreQuery<string>(sql).ToList();
            a.Genres = query4;

            a.Rating = float.Parse(item.fRating, CultureInfo.InvariantCulture.NumberFormat);
            a.Writers = (item.strCredits).Split('/').Select(x => x.Trim()).ToList();
            a.DateAdded = item.dateAdded;

            result.Add(a);
          }
          return result;
        }

        public WebMovieBasic GetMovieBasicById(string movieId)
        {
          string sql = "SELECT * FROM movieinfo where idMovie = '" + movieId + "'";

          var query = _connection.ExecuteStoreQuery<movieinfo>(sql).FirstOrDefault();

          WebMovieBasic result = new WebMovieBasic();
          result.Id = query.idMovie.ToString();

          sql = "SELECT CONCAT(p.strPath, f.strFilename) AS fullpath FROM files f, path p where p.idPath = f.idPath and f.idMovie = " + movieId;
          var query2 = _connection.ExecuteStoreQuery<string>(sql).ToList();
          result.Path = query2;
          result.Title = query.strTitle;
          result.Rating = float.Parse(query.fRating, CultureInfo.InvariantCulture.NumberFormat);

          return result;
        }

        public WebMovieDetailed GetMovieDetailedById(string movieId)
        {
          string sql = "SELECT * FROM movieinfo where idMovie = '" + movieId + "'";

          var query = _connection.ExecuteStoreQuery<movieinfo>(sql).FirstOrDefault();

          WebMovieDetailed result = new WebMovieDetailed();
          result.Id = query.idMovie.ToString();

          sql = "SELECT CONCAT(p.strPath, f.strFilename) AS fullpath FROM files f, path p where p.idPath = f.idPath and f.idMovie = " + movieId;
          var query2 = _connection.ExecuteStoreQuery<string>(sql).ToList();
          result.Path = query2;
          result.Title = query.strTitle;
          result.Year = (int)query.iYear;
          result.Runtime = (int)query.runtime;
          result.Summary = query.strPlot;
          result.Watched = Convert.ToBoolean(query.iswatched);
          result.Summary = query.strPlot;
          result.Title = query.strTitle;

          sql = "SELECT strActor FROM actorlinkmovie alm, actors a where alm.idActor = a.idActor and alm.idMovie =" + movieId;
          var query3 = _connection.ExecuteStoreQuery<string>(sql).ToList();
          var actr = new List<WebActor>();

          foreach (WebActor item2 in query3)
          {
            WebActor a2 = new WebActor();
            a2.Title = item2;
            actr.Add(a2);
          }
          result.Actors = actr;

          sql = "SELECT strGenre FROM genrelinkmovie glm, genre g where glm.idGenre = g.idGenre and glm.idMovie =" + movieId;
          var query4 = _connection.ExecuteStoreQuery<string>(sql).ToList();
          result.Genres = query4;

          result.Rating = float.Parse(query.fRating, CultureInfo.InvariantCulture.NumberFormat);
          result.Writers = (query.strCredits).Split('/').Select(x => x.Trim()).ToList();
          result.DateAdded = query.dateAdded;
        
          return result;
        }

        public IEnumerable<WebGenre> GetAllGenres()
        {
          string sql = "SELECT * FROM genre";

          var query = _connection.ExecuteStoreQuery<genre>(sql).ToList();

          var result = new List<WebGenre>();

          foreach (genre item in query)
          {
            WebGenre a = new WebGenre();
            a.Title = item.strGenre;
            result.Add(a);
          }
          return result;
        }

        public IEnumerable<WebCategory> GetAllCategories()
        {
            return new List<WebCategory>();
        }

        public WebFileInfo GetFileInfo(string path)
        {
            if (path.StartsWith("http://"))
            {
                return ArtworkRetriever.GetFileInfo(path);
            }

            return new WebFileInfo(PathUtil.StripFileProtocolPrefix(path));
        }

        public Stream GetFile(string path)
        {
            if (path.StartsWith("http://"))
            {
                return ArtworkRetriever.GetStream(path);
            }

            return new FileStream(PathUtil.StripFileProtocolPrefix(path), FileMode.Open, FileAccess.Read);
        }

        public IEnumerable<WebSearchResult> Search(string text)
        {
            using (DatabaseConnection connection = OpenConnection())
            {
                var param = new SQLiteParameter("@search", "%" + text + "%");
                string sql = "SELECT idMovie, strTitle, iYear, strGenre FROM movieinfo WHERE strTitle LIKE @search";
                IEnumerable<WebSearchResult> titleResults = ReadList<WebSearchResult>(sql, delegate(SQLiteDataReader reader)
                {
                    string title = reader.ReadString(1);
                    string genres = reader.ReadString(3);
                    return new WebSearchResult()
                    {
                        Type = WebMediaType.Movie,
                        Id = reader.ReadIntAsString(0),
                        Title = title,
                        Score = (int)Math.Round(40 + (decimal)text.Length / title.Length * 40),
                        Details = new WebDictionary<string>()
                    {
                        { "Year", reader.ReadIntAsString(2) },
                        { "Genres", genres == "unknown" ? String.Empty : genres }
                    }
                    };
                }, param);

                string actorSql = "SELECT a.strActor, mi.idMovie, mi.strTitle, mi.iYear, mi.strGenre " +
                                  "FROM actors a " +
                                  "LEFT JOIN actorlinkmovie alm ON alm.idActor = a.idActor " +
                                  "INNER JOIN movieinfo mi ON alm.idMovie = mi.idMovie " +
                                  "WHERE a.strActor LIKE @search";
                IEnumerable<WebSearchResult> actorResults = ReadList<WebSearchResult>(actorSql, delegate(SQLiteDataReader reader)
                {
                    string genres = reader.ReadString(4);
                    return new WebSearchResult()
                    {
                        Type = WebMediaType.Movie,
                        Id = reader.ReadIntAsString(1),
                        Title = reader.ReadString(2),
                        Score = (int)Math.Round(40 + (decimal)text.Length / reader.ReadString(0).Length * 30),
                        Details = new WebDictionary<string>()
                    {
                        { "Year", reader.ReadIntAsString(3) },
                        { "Genres", genres == "unknown" ? String.Empty : genres }
                    }
                    };
                }, param);

                return titleResults.Concat(actorResults);
            }
        }

        public WebDictionary<string> GetExternalMediaInfo(WebMediaType type, string id)
        {
            return new WebDictionary<string>()
            {
                { "Type", "myvideos" },
                { "Id", id }
            };
        }
    }
}
