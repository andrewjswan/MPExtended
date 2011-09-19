﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
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
using System.Text;
using System.Data.SQLite;

namespace MPExtended.Libraries.SQLitePlugin
{
    public static class DataReaders
    {
        public static object ReadInt32(SQLiteDataReader reader, int idx)
        {
            try
            {
                if (reader.IsDBNull(idx))
                    return 0;
                else
                    return reader.GetInt32(idx);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static object ReadString(SQLiteDataReader reader, int idx)
        {
            try
            {
                if (reader.IsDBNull(idx))
                    return "";
                else
                    return reader.GetString(idx);
            }
            catch (InvalidCastException)
            {
                // This is a workaround for the weird bug that for some reason numeric values throw an InvalidCastException. 
                // See for example http://forum.team-mediaportal.com/webservice-mobile-access-537/webservice-general-media-access-webservice-89956/index7.html#post748640
                return ReadInt32(reader, idx).ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static object ReadBoolean(SQLiteDataReader reader, int idx)
        {
            try
            {
                if (reader.IsDBNull(idx))
                    return false;
                else
                    return reader.GetBoolean(idx);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static object ReadDateTime(SQLiteDataReader reader, int idx)
        {
            try
            {
                if (reader.IsDBNull(idx))
                    return new DateTime(1970, 1, 1);
                else
                    return DateTime.Parse(reader.GetString(idx));
            }
            catch (Exception)
            {
                return new DateTime(1970, 1, 1);
            }
        }

        public static object ReadFloat(SQLiteDataReader reader, int idx)
        {
            try
            {
                if (reader.IsDBNull(idx))
                    return 0;
                else
                    return reader.GetFloat(idx);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static object ReadList(SQLiteDataReader reader, int idx, char separator)
        {
            string txt = (string)ReadString(reader, idx);
            if (txt.Length == 0)
                return new List<string>();

            return txt.Split(separator).Where(y => y.Trim().Length > 0).ToList();
        }

        public static object ReadPipeList(SQLiteDataReader reader, int idx)
        {
            return ReadList(reader, idx, '|');
        }

        public static object ReadIntAsString(SQLiteDataReader reader, int idx)
        {
            return ReadInt32(reader, idx).ToString();
        }

        public static object ReadStringAsInt(SQLiteDataReader reader, int idx)
        {
            return Int32.Parse((string)ReadString(reader, idx));
        }

        public static object ReadStringAsList(SQLiteDataReader reader, int idx)
        {
            return new List<string>() { (string)ReadString(reader, idx) };
        }
    }
}
