﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RuneApp.InternalServer
{
	public abstract class PageRenderer
	{
		public abstract HttpResponseMessage Render(HttpListenerRequest req, string[] uri);

		protected virtual HttpResponseMessage Recurse(HttpListenerRequest req, string[] uri)
		{
			Master.Log.Debug("recursing " + uri.Length + " " + uri.FirstOrDefault());
			if (uri.Length == 0)
				return null;

			var types = this.GetType().GetNestedTypes();
			var atypes = types.Where(t => (t.GetCustomAttribute(typeof(Master.PageAddressRenderAttribute)) is Master.PageAddressRenderAttribute));
			var type = atypes.FirstOrDefault(t => (t.GetCustomAttribute(typeof(Master.PageAddressRenderAttribute)) as Master.PageAddressRenderAttribute).PageAddress == uri.First());
			if (type != null)
			{
				var tt = type.GetConstructor(new Type[] { }).Invoke(new object[] { });
				return (HttpResponseMessage)type.GetMethod("Render").Invoke(tt, new object[] { req, uri.Skip(1).ToArray() });
			}
			return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("<html><body>404: " + req.RawUrl + " not found. <a href='/'>Return.</a></body></html>") };
		}
	}

	public class ServedResult
	{
		public string name;

		public bool isList = false;
		public Dictionary<string, ServedResult> contentDic = new Dictionary<string, ServedResult>();
		public List<ServedResult> contentList = new List<ServedResult>();

		public ServedResult()
		{
		}

		public ServedResult(bool listify) : base()
		{
			isList = listify;
		}

		public ServedResult(string n) : base()
		{
			name = n;
			if (name == "script")
				contentList.Add("");
		}

		public virtual string ToJson()
		{
			if (isList)
				return "[" + string.Join(",", string.Join("", contentList.Select(li => li.ToJson()))) + "]";
			else
				return "{" + string.Join(",", contentDic.Select(kv => '"' + kv.Key + "\":" + kv.Value.ToJson())) + "}";
		}

		public virtual string ToHtml()
		{
			return "<" + name
					+ (contentDic.Count > 0 ? " " : "") + string.Join(" ", contentDic.Select(kv => kv.Key + "=" + kv.Value))
					+ (contentList.Count == 0 ? "/" : "") + ">" + (contentList.Count > 0 ? "\n" : "")
					+ string.Join("\n", contentList.Select(li => li.ToHtml())) + (contentList.Count > 0 ? "\n" : "")
					+ (contentList.Count > 0 ? ("</" + name + ">") : "");
		}

		public static implicit operator ServedResult(string rhs)
		{
			return (ServedString)rhs;
		}
	}

	public class ServedString : ServedResult
	{
		readonly string value = null;

		public ServedString(string v)
		{
			value = v;
		}

		public static implicit operator ServedString(string rhs)
		{
			return new ServedString(rhs);
		}

		public static implicit operator string(ServedString rhs)
		{
			return rhs.value;
		}

		public override string ToHtml()
		{
			return value;
		}

		public override string ToJson()
		{
			return "\"" + value + "\"";
		}

		public override string ToString()
		{
			return value;
		}
	}

	public static class ServerExtensions
	{
		public static string getHeadOrParam(this HttpListenerRequest req, string name)
		{
			if (req.QueryString.AllKeys.Contains(name))
				return req.QueryString[name];

			if (req.Headers.AllKeys.Contains(name))
				return req.Headers[name];

			return null;
		}
	}

	public class FileContent : ByteArrayContent
	{
		public readonly string FileName;
		public readonly string Type;

		public FileContent(string fname, byte[] content, string type = "application/octet-stream") : base(content)
		{
			this.FileName = fname;
			this.Type = type;
		}
	}

}
