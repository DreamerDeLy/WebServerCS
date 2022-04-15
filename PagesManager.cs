using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace WebServer
{
	delegate void WebPageHandler(HttpListenerResponse response);

	class WebPage
	{
		public WebPage(string path, string method, WebPageHandler handler)
		{
			Path = path;
			Handler = handler;
			Method = method;
		}

		public string Path { get; }
		public string Method { get; }
		public WebPageHandler Handler { get; }
	}

	class PagesManager
	{
		public static void Init()
		{
			Server.On("/", PageIndex);
			Server.On("/lol", PageLol);
		}

		public static void ErrorPage(HttpListenerResponse response, int code)
		{
			Logger.Warning("Client", "Sending error to client: " + code);

			string html = File.ReadAllText("www/templates/error.html");

			html = html.Replace("{error_code}", code.ToString());
			html = html.Replace("{error_message}", ((HttpStatusCode)code).ToString());

			SendPage(response, html);
		}

		private static string GetTemplate(string fileName)
		{
			try
			{
				return File.ReadAllText("www/templates/" + fileName);
			}
			catch (Exception e)
			{	
				Logger.Error("Pages", "Open template file error: " + e.Message);
				return String.Empty;
			}
		}

		private static void SendPage(HttpListenerResponse response, string html)
		{
			// Check if HTML is valid
			if (html == String.Empty)
			{
				ErrorPage(response, 500);
				return;
			}

			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);

			response.ContentLength64 = buffer.Length;
			response.ContentType = "text/html";

			Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}

		private static void PageIndex(HttpListenerResponse response)
		{
			string content = "<p>Короче, тут буде головна сторінка смартхому, але поки тут пусто)))</p>";

			string html = GetTemplate("template.html");

			html = html.Replace("{page_name}", "Home");
			html = html.Replace("{content}", content);

			SendPage(response, html);
		}

		private static void PageLol(HttpListenerResponse response)
		{
			string content = "Не ну це просто LOL...";

			string html = GetTemplate("template.html");

			html = html.Replace("{page_name}", "Lol");
			html = html.Replace("{content}", content);

			SendPage(response, html);
		}
	}
}
