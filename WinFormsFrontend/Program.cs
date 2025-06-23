using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Database;
using BrokenStatsFrontendWinForms.Services;

namespace BrokenStatsFrontendWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=data/data.db"));
            services.AddHttpClient<HttpBackendService>(c => c.BaseAddress = new Uri("http://localhost:5005"));
            services.AddTransient<LocalBackendService>();
            services.AddTransient<IBackendService>(sp =>
            {
                return Environment.GetEnvironmentVariable("LOCAL_BACKEND") == "1"
                    ? sp.GetRequiredService<LocalBackendService>()
                    : sp.GetRequiredService<HttpBackendService>();
            });
            services.AddTransient<MainForm>();

            using var provider = services.BuildServiceProvider();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = provider.GetRequiredService<MainForm>();
            Application.Run(form);
        }
    }
}
