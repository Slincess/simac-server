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
        CancellationTokenSource? cs;
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
            app.MapGet("api/StartServer", () =>
            {
                //server.analytics = analytics;
                cs = new();
                serverTask = Task.Run(() => server.Run(cs.Token), cs.Token);
                isRunning = true;
                //return Results.Ok(true);
            });
            app.MapGet("api/CloseServer", async () =>
            {
                await server.StopServer();
                cs?.Cancel();
                isRunning = false;
                //return Results.Ok(true);
            });
            app.MapGet("api/running", () =>
            {
                return isRunning;
            });

            app.MapGet("/api/GetCCU", async context =>
            {
                context.Response.Headers.Add("Content-Type", "text/event-stream");

                while (true)
                {
                    string CCUJson = S_analytics.Instance.GetCCU_Json();
                    await context.Response.WriteAsync($"data: {CCUJson}\n\n");
                    await context.Response.Body.FlushAsync();
                    await Task.Delay(5000);
                }
            });

            app.MapGet("/api/GetMessage", async context =>
            {
                context.Response.Headers.Add("Content-Type", "text/event-stream");

                while (true)
                {
                    string MessagesJson = S_analytics.Instance.GetMessages_Json();
                    await context.Response.WriteAsync($"data: {MessagesJson}\n\n");
                    await context.Response.Body.FlushAsync();
                    await Task.Delay(5000);
                }
            });

             


            await Task.Run(() => app.Run("http://0.0.0.0:5001"));

        }
    }
}
