using System.Net;
using System.Net.Sockets;
using TaskRunner.Core.Operations;
using TaskRunner.Core.Operations.Implementations;
using TaskRunner.Core.Tasks;
using TaskRunner.Core.Tasks.Implementations;
using TaskRunner.Host;
using TaskRunner.Host.Hubs;
using TaskRunner.Host.Services;

// Find an available port
var port = GetAvailablePort(5050);
var url = $"http://localhost:{port}";

// Start the web server in a background thread
var webServerThread = new Thread(() => StartWebServer(args, url))
{
    IsBackground = true
};
webServerThread.Start();

// Give the server a moment to start
Thread.Sleep(800);

// Run the Windows Forms application on a dedicated STA thread
var uiThread = new Thread(() =>
{
    Application.SetHighDpiMode(HighDpiMode.SystemAware);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm(url));
});
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

// When the form closes, the application will exit
Environment.Exit(0);

static int GetAvailablePort(int preferredPort)
{
    // Try the preferred port first
    if (IsPortAvailable(preferredPort))
        return preferredPort;

    // Find any available port
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static bool IsPortAvailable(int port)
{
    try
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch
    {
        return false;
    }
}

static void StartWebServer(string[] args, string url)
{
    var builder = WebApplication.CreateBuilder(args);

    // Set content root to the exe's directory so wwwroot is found
    // regardless of the working directory
    var exeDir = AppContext.BaseDirectory;
    builder.Environment.ContentRootPath = exeDir;
    builder.Environment.WebRootPath = Path.Combine(exeDir, "wwwroot");

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddSignalR();

    // Register background tasks
    builder.Services.AddSingleton<IBackgroundTask, FileProcessingTask>();
    builder.Services.AddSingleton<IBackgroundTask, DataSyncTask>();
    builder.Services.AddSingleton<IBackgroundTask, DirectorySizeTask>();
    builder.Services.AddSingleton<IBackgroundTask, FindFilesTask>();

    // Register synchronous operations
    builder.Services.AddSingleton<ISyncOperation, CalculatorOperation>();
    builder.Services.AddSingleton<ISyncOperation, SystemInfoOperation>();
    builder.Services.AddSingleton<ISyncOperation, GetDrivesOperation>();
    builder.Services.AddSingleton<ISyncOperation, DeleteFilesOperation>();

    // Register execution services
    builder.Services.AddSingleton<TaskExecutionService>();
    builder.Services.AddSingleton<OperationExecutionService>();

    // Configure CORS for development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174", url)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    app.UseCors();
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapControllers();
    app.MapHub<TaskHub>("/hubs/tasks");

    app.Urls.Add(url);

    Console.WriteLine($"Web server starting at {url}");
    app.Run();
}

