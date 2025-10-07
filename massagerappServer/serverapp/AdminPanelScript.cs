using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace serverapp
{
    public class AdminPanelScript
    {
        serverR server = new();
        Task? serverTask;
        CancellationTokenSource? serverCts;
        private bool isRunning;
        public async Task Run()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            WebApplication app = builder.Build();

            app.UseStaticFiles();


            app.MapGet("/", context =>
            {
                context.Response.Redirect("/index.html");
                return Task.CompletedTask;
            });
            app.MapGet("api/running", () =>
            {
                return isRunning;
            });

            app.MapGet("/api/GetCCU", async context =>
            {
                context.Response.Headers.Add("Content-Type", "text/event-stream");
                try
                {
                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        string CCUJson = S_analytics.Instance.GetCCU_Json();
                        await context.Response.WriteAsync($"data: {CCUJson}\n\n");
                        await context.Response.Body.FlushAsync();


                        try
                        {
                            await Task.Delay(5000, context.RequestAborted);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }
                catch
                {

                    throw;
                }
            });

            app.MapGet("/api/GetMessage", async context =>
            {
                context.Response.Headers.Add("Content-Type", "text/event-stream");


                while (!context.RequestAborted.IsCancellationRequested)
                {
                    string MessagesJson = S_analytics.Instance.GetMessages_Json();
                    await context.Response.WriteAsync($"data: {MessagesJson}\n\n");
                    await context.Response.Body.FlushAsync();

                    try
                    {
                        await Task.Delay(5000, context.RequestAborted);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            });

            app.MapPost("/api/UploadImage", async (HttpRequest request) =>
            {
                S_analytics.Instance.SaveInfo();
                if (!request.HasFormContentType)
                    return Results.BadRequest("Invalid form data");

                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("png");

                if (file == null || file.Length == 0)
                    return Results.BadRequest("No file uploaded");

                string key = S_analytics.Instance.AddImage(file);

                return Results.Ok(key);
            });

            app.MapPost("/api/GetImage", async (HttpRequest request) =>
            {

                if (!request.HasFormContentType)
                    return Results.BadRequest("Invalid form data");

                var form = await request.ReadFormAsync();
                var file = form["key"];

                byte[] picture_byte = S_analytics.Instance.GetImage(file);

                if (picture_byte == null)
                {
                    return Results.NotFound();
                }

                return Results.File(picture_byte, "image/png", file + ".png");
            });
            app.MapGet("/api/StartServer", () =>
            {
                if (isRunning)
                    return Results.Conflict("Server already running");

                serverCts = new CancellationTokenSource();
                serverTask = Task.Run(() => server.Run(serverCts.Token), serverCts.Token);
                isRunning = true;
                return Results.Ok(true);
            });

            app.MapGet("/api/CloseServer", async () =>
            {
                if (!isRunning)
                    return Results.BadRequest("Server not running");
                await server.StopServer();
                serverCts?.Cancel();
                try
                {
                    serverTask.Dispose();
                    if (serverTask != null)
                        await serverTask;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine("server stop exception: " + ex);
                }

                serverTask = null;
                serverCts?.Dispose();
                serverCts = null;
                isRunning = false;

                return Results.Ok(true);
            });

            await Task.Run(() => app.Run("http://0.0.0.0:5001"));

        }
    }
}
