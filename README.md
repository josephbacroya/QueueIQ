# QueueIQ

QueueIQ is a real-time, AI-powered queue management system built end-to-end with .NET 9.

## Features
- **ASP.NET Core API**: A clean, RESTful backend powered by Entity Framework Core (SQLite).
- **Blazor Server UI**: A real-time, Apple-inspired frontend.
- **SignalR Integration**: Instant, live updates broadcasted to all connected clients when the queue changes.
- **Machine Learning (ML.NET)**: Custom-trained models that predict accurate wait times (regression) and flag high-risk "no-shows" (classification).
- **Enterprise Observability**: Structured JSON logging via Serilog and global exception handling.

---

## How to Start Locally

We have created a custom PowerShell orchestrator script that automatically boots up both the Backend API and the Frontend Web App simultaneously, and streams their logs into a single terminal window.

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed.

### Running the Application

1. Open a PowerShell terminal at the root of the `QueueIQ` repository.
2. Run the orchestrator script:
   ```powershell
   .\start-queueiq.ps1
   ```
3. Wait for the terminal to show that both applications have started. You will see API logs in magenta and Web App logs in green.

### Accessing the Application

Once the script is running, open your browser and navigate to:

- **Customer View (Join Queue):**  
  [http://localhost:5199/join/joes-barbershop](http://localhost:5199/join/joes-barbershop)
  
- **Staff Dashboard:**  
  [http://localhost:5199/staff/joes-barbershop](http://localhost:5199/staff/joes-barbershop)

- **API Swagger UI:**  
  [http://localhost:5088/swagger](http://localhost:5088/swagger)

*Tip: Open the Customer View and Staff Dashboard side-by-side in your browser to see the real-time SignalR updates and micro-animations in action.*

### Stopping the Application

To shut down the system gracefully, go back to the PowerShell terminal running the script and press:
**`Ctrl + C`**

The script will automatically intercept the command, shut down the background processes, and free up the network ports.
