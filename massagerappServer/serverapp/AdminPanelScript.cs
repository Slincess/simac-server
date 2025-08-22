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
                cs = new();
                serverTask = Task.Run(() => server.Run(cs.Token), cs.Token);

            });
            app.MapGet("api/CloseServer", async () =>
            {
                await server.StopServer();
                cs?.Cancel();
                
            });

            await Task.Run(() => app.Run("http://localhost:5001"));

        }
    }
}
