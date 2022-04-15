using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace WebServer
{
	class Server
	{
		public static int maxSimultaneousConnections = 20;
		private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

		static private List<WebPage> _WebPages = new List<WebPage>();

		public static async Task Start()
		{
			await Server.Listen();
		}

		public static void On(string path, string method, WebPageHandler handler)
		{
			_WebPages.Add(new WebPage(path, method, handler));
		}

		public static void On(string path, WebPageHandler handler)
		{
			_WebPages.Add(new WebPage(path, "GET", handler));
			_WebPages.Add(new WebPage(path, "POST", handler));
		}

		// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
		private static List<IPAddress> GetLocalHostIPs()
		{
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());
			List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();

			return ret;
		}

		private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
		{
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:8080/");

			// Listen to IP address as well.
			localhostIPs.ForEach(ip =>
			{
				string address = "http://" + ip.ToString() + ":8080/";
				Logger.Print("Server", "Listening on IP " + address);
				listener.Prefixes.Add(address);
			});

			return listener;
		}

		private static async Task Listen()
		{
			HttpListener listener = InitializeListener(GetLocalHostIPs());
			listener.Start();

			while (true)
			{
				// Semaphore
				sem.WaitOne();

				// Get request
				HttpListenerContext context = await listener.GetContextAsync();
				HttpListenerRequest request = context.Request;
				HttpListenerResponse response = context.Response;

				Logger.Info("Server", request.RemoteEndPoint + " " + request.HttpMethod + " " + request.Url.AbsolutePath);

				// Find non-static pages
				if (_WebPages.Any(p => p.Path == request.Url.AbsolutePath && p.Method == request.HttpMethod))
				{
					WebPage w = _WebPages.First(p => p.Path == request.Url.AbsolutePath && p.Method == request.HttpMethod);

					Logger.Print("Server", "Sending non-static page");

					w.Handler.Invoke(response);
				}
				else
				{
					if (request.HttpMethod == "GET")
					{
						ServeStatic(request, response);
					}
					else
					{
						Stream output = response.OutputStream;

						// End connection
						output.Close();
					}
				}
			}
		}

		private static void ServeStatic(HttpListenerRequest request, HttpListenerResponse response)
		{
			// If request contains "::" send error 400 (for example: http://example.com/../../file.txt)
			if (request.Url.AbsolutePath.IndexOf("..") >= 0)
			{
				Logger.Error("Client", "URI contains \"::\" symbol");
				PagesManager.ErrorPage(response, 400);
				return;
			}

			string filePath = "www/static" + request.Url.AbsolutePath;

			// Add index.html to uri with "/" at the end
			if (filePath.EndsWith("/"))
			{
				filePath += "index.html";
			}

			// If file npt exist
			if (!File.Exists(filePath))
			{
				PagesManager.ErrorPage(response, 404);
				return;
			}

			// Get extension from filename
			string fileExtension = filePath.Substring(filePath.LastIndexOf('.'));

			// File ContentType
			string contentType = GetContentType(fileExtension);

			try
			{
				// File content 
				byte[] buffer = File.ReadAllBytes(filePath);

				// Set headers
				response.ContentLength64 = buffer.Length;
				response.ContentType = contentType;

				Logger.Print("Server", "Response with content-type: " + contentType);

				// Send data
				Stream output = response.OutputStream;
				output.Write(buffer, 0, buffer.Length);

				// End connection
				output.Close();
			}
			catch (Exception e)
			{
				Logger.Error("Server", "Response error: " + e.Message);

				// Send 500 error in case of exception
				PagesManager.ErrorPage(response, 500);
				return;
			}
		}

		private static string GetContentType(string fileExtension)
		{
			string contentType = "";

			// Get ContentType from extension
			switch (fileExtension)
			{
				case ".htm":
				case ".html":
					contentType = "text/html";
					break;
				case ".css":
					contentType = "text/css";
					break;
				case ".js":
					contentType = "text/javascript";
					break;
				case ".jpg":
					contentType = "image/jpeg";
					break;
				case ".svg":
					contentType = "image/svg+xml";
					break;
				case ".jpeg":
				case ".png":
				case ".gif":
					contentType = "image/" + fileExtension.Substring(1);
					break;
				default:
					if (fileExtension.Length > 1)
					{
						contentType = "application/" + fileExtension.Substring(1);
					}
					else
					{
						contentType = "application/unknown";
					}
					break;
			}

			return contentType;
		}
	}
}
