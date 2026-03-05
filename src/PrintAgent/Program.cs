using MannaHp.PrintAgent;
using MannaHp.PrintAgent.Data;
using Microsoft.EntityFrameworkCore;
using Sentry;

var builder = Host.CreateApplicationBuilder(args);

// Sentry error tracking
SentrySdk.Init(o =>
{
	o.Dsn = builder.Configuration["Sentry:Dsn"] ?? "";
	o.TracesSampleRate = 0.1;
	o.SendDefaultPii = false;
});

builder.Services.AddDbContext<PrintAgentDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
