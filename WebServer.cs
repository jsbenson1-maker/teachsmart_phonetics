using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PhoneticsEdu
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly string _webAssetsPath;
        private bool _isRunning;

        public WebServer(string path, int port)
        {
            _webAssetsPath = path;
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            _isRunning = true;
            _listener.Start();
            Task.Run(() => ListenLoop());
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                _listener.Stop();
            }
            catch { }
        }

        private async Task ListenLoop()
        {
            while (_isRunning && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Fail silently or log minimally to prevent polluting command output
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string path = request.Url.LocalPath.TrimStart('/');
                if (string.IsNullOrEmpty(path)) path = "index.html";

                string filePath = Path.Combine(_webAssetsPath, path);
                
                // Security check to avoid path traversal
                string fullPath = Path.GetFullPath(filePath);
                string fullAssetsPath = Path.GetFullPath(_webAssetsPath);
                if (!fullPath.StartsWith(fullAssetsPath, StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.OutputStream.Close();
                    return;
                }

                if (File.Exists(filePath))
                {
                    byte[] buffer = File.ReadAllBytes(filePath);
                    response.ContentLength64 = buffer.Length;
                    string ext = Path.GetExtension(filePath).ToLower();
                    response.ContentType = ext switch
                    {
                        ".html" => "text/html; charset=utf-8",
                        ".css" => "text/css",
                        ".js" => "application/javascript",
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".svg" => "image/svg+xml",
                        ".ico" => "image/x-icon",
                        _ => "application/octet-stream"
                    };
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                try
                {
                    using var writer = new StreamWriter(response.OutputStream);
                    writer.Write($"Server error: {ex.Message}");
                }
                catch { }
            }
            finally
            {
                try
                {
                    response.OutputStream.Close();
                }
                catch { }
            }
        }
    }
}
