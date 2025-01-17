﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Globalization;
using EW.Graphics;
using System.ComponentModel;

namespace EW
{
    public static class FieldSaver
    {
        public static MiniYaml Save(object o, bool includePrivateByDefault = false)
        {
            var nodes = new List<MiniYamlNode>();
            string root = null;

            foreach (var info in FieldLoader.GetTypeLoadInfo(o.GetType(), includePrivateByDefault))
            {
                if (info.Attribute.DictionaryFromYamlKey)
                {
                    var dict = (System.Collections.IDictionary)info.Field.GetValue(o);
                    foreach (var kvp in dict)
                    {
                        var key = ((System.Collections.DictionaryEntry)kvp).Key;
                        var value = ((System.Collections.DictionaryEntry)kvp).Value;

                        nodes.Add(new MiniYamlNode(FormatValue(key), FormatValue(value)));
                    }
                }
                else if (info.Attribute.FromYamlKey)
                    root = FormatValue(o, info.Field);
                else
                    nodes.Add(new MiniYamlNode(info.YamlName, FormatValue(o, info.Field)));
            }

            return new MiniYaml(root, nodes);
        }

        public static MiniYaml SaveDifferences(object o, object from, bool includePrivateByDefault = false)
        {
            if (o.GetType() != from.GetType())
                throw new InvalidOperationException("FieldLoader: can't diff objects of different types");

            var fields = FieldLoader.GetTypeLoadInfo(o.GetType(), includePrivateByDefault)
                .Where(info => FormatValue(o, info.Field) != FormatValue(from, info.Field));

            return new MiniYaml(
                null,
                fields.Select(info => new MiniYamlNode(info.YamlName, FormatValue(o, info.Field))).ToList());
        }

        public static MiniYamlNode SaveField(object o, string field)
        {
            return new MiniYamlNode(field, FormatValue(o, o.GetType().GetField(field)));
        }

        public static string FormatValue(object v)
        {
            if (v == null)
                return "";

            var t = v.GetType();

            // Color.ToString() does the wrong thing; force it to format as rgb[a] hex
            if (t == typeof(Color))
            {
                return HSLColor.ToHexString((Color)v);
            }

            // HSLColor.ToString() does the wrong thing; force it to format as rgb[a] hex
            if (t == typeof(HSLColor))
            {
                return ((HSLColor)v).ToHexString();
            }

            //if (t == typeof(ImageFormat))
            //{
            //    return ((ImageFormat)v).ToString();
            //}

            if (t == typeof(Rectangle))
            {
                var r = (Rectangle)v;
                return "{0},{1},{2},{3}".F(r.X, r.Y, r.Width, r.Height);
            }

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                return ((Array)v).Cast<object>().Select(FormatValue).JoinWith(", ");
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return ((System.Collections.IEnumerable)v).Cast<object>().Select(FormatValue).JoinWith(", ");
            }

            // This is only for documentation generation
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var result = "";
                var dict = (System.Collections.IDictionary)v;
                foreach (var kvp in dict)
                {
                    var key = ((System.Collections.DictionaryEntry)kvp).Key;
                    var value = ((System.Collections.DictionaryEntry)kvp).Value;

                    var formattedKey = FormatValue(key);
                    var formattedValue = FormatValue(value);

                    result += "{0}: {1}{2}".F(formattedKey, formattedValue, Environment.NewLine);
                }

                return result;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Primitives.Cache<,>))
                return ""; // TODO

            if (t == typeof(DateTime))
                return ((DateTime)v).ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

            // Try the TypeConverter
            var conv = TypeDescriptor.GetConverter(t);
            if (conv.CanConvertTo(typeof(string)))
            {
                try
                {
                    return conv.ConvertToInvariantString(v);
                }
                catch
                {
                }
            }

            return v.ToString();
        }

        public static string FormatValue(object o, FieldInfo f)
        {
            return FormatValue(f.GetValue(o));
        }
    }
}