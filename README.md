# Task Runner Application

A Windows desktop application with a Vue 3 frontend and ASP.NET Core backend that demonstrates:
- **Long-running tasks** with real-time progress updates via SignalR
- **Synchronous operations** for quick request/response interactions
- **Single executable deployment** for easy distribution

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    TaskRunner.Host                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                 ASP.NET Core Backend                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │    │
│  │  │ TasksAPI    │  │ OperationsAPI│  │  SignalR    │  │    │
│  │  │ Controller  │  │ Controller   │  │  TaskHub    │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │    │
│  │         │                │                │          │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │              Execution Services              │    │    │
│  │  │  TaskExecutionService  OperationExecutionSvc │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────┘    │
│                            │                                 │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                Vue 3 + Vite Frontend                 │    │
│  │              (Served from wwwroot)                   │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              │       TaskRunner.Core       │
              │  ┌─────────────────────┐    │
              │  │    Abstractions     │    │
              │  │  IBackgroundTask    │    │
              │  │  ISyncOperation     │    │
              │  └─────────────────────┘    │
              │  ┌─────────────────────┐    │
              │  │   Implementations   │    │
              │  │  FileProcessingTask │    │
              │  │  DataSyncTask       │    │
              │  │  CalculatorOperation│    │
              │  │  SystemInfoOperation│    │
              │  └─────────────────────┘    │
              └─────────────────────────────┘
```

## Projects

### TaskRunner.Core
Contains the shared abstractions and implementations:
- **`IBackgroundTask`** - Interface for long-running tasks with progress reporting
- **`ISyncOperation`** - Interface for synchronous operations
- **`TaskProgress`** - Progress data model with customizable fields
- **Sample Tasks**: FileProcessingTask, DataSyncTask
- **Sample Operations**: CalculatorOperation, SystemInfoOperation

### TaskRunner.Host
The main executable that hosts:
- ASP.NET Core web server
- SignalR hub for real-time communication
- REST API controllers
- Vue 3 frontend (embedded in wwwroot)

## Adding New Tasks

1. Create a class implementing `IBackgroundTask`:

```csharp
public class MyCustomTask : IBackgroundTask
{
    public string TaskType => "my-custom-task";
    public string DisplayName => "My Custom Task";

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        // Report progress
        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Processing",
            PercentComplete = 50,
            CustomData = new() { ["myData"] = "value" }
        });

        // Do work...

        return TaskResult.Successful("Done!");
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddSingleton<IBackgroundTask, MyCustomTask>();
```

## Adding New Operations

1. Create a class implementing `ISyncOperation`:

```csharp
public class MyOperation : ISyncOperation
{
    public string OperationType => "my-operation";
    public string DisplayName => "My Operation";

    public Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters)
    {
        // Do work synchronously
        return Task.FromResult(OperationResult.Successful("Result"));
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddSingleton<ISyncOperation, MyOperation>();
```

## Development

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+

### Running in Development

1. Build the frontend:
   ```bash
   cd src/TaskRunner.Host/frontend
   npm install
   npm run build
   ```

2. Run from Visual Studio:
   - Open `TaskRunner.slnx`
   - Set `TaskRunner.Host` as startup project
   - Press F5

3. Or run from command line:
   ```bash
   dotnet run --project src/TaskRunner.Host
   ```

### Frontend Development with Hot Reload

1. Start the backend:
   ```bash
   dotnet run --project src/TaskRunner.Host
   ```

2. In another terminal, start Vite dev server:
   ```bash
   cd src/TaskRunner.Host/frontend
   npm run dev
   ```

3. Open http://localhost:5173 (Vite proxies API calls to backend)

## Publishing

Create a single executable:

```bash
dotnet publish src/TaskRunner.Host -c Release -o publish
```

The output will be in the `publish` folder with a single `TaskRunner.Host.exe` file.

## API Endpoints

### Tasks
- `GET /api/tasks/types` - Get available task types
- `GET /api/tasks/running` - Get currently running tasks
- `POST /api/tasks/start` - Start a new task
- `POST /api/tasks/{taskId}/cancel` - Cancel a running task

### Operations
- `GET /api/operations/types` - Get available operation types
- `POST /api/operations/execute` - Execute an operation

### SignalR Hub
- `/hubs/tasks` - Real-time task progress updates
  - Events: `TaskStarted`, `TaskProgress`, `TaskCompleted`, `TaskCancelled`, `TaskFailed`
  - Methods: `SubscribeToTask(taskId)`, `UnsubscribeFromTask(taskId)`
