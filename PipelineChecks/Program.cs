

using Microsoft.AspNetCore.Mvc;
using PipelineChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.MapPost("/SyncCheck", static (HttpRequest request) =>
{
    return "Completed";
})
.WithOpenApi();


async static Task CheckPipelineCompliancyAsync(HttpContext context)
{
    var taskProperties = TaskProperties.GetTaskProperties(context.Request.Headers);

    var executionEngine = new TaskExecution();
    _ = Task.Run(() => executionEngine.ExecuteAsync(taskProperties, new CancellationToken()));

    await Task.CompletedTask;

    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync("Request accepted!");
}

app.MapPost("/AsyncCheck", (Delegate)CheckPipelineCompliancyAsync).WithOpenApi();




app.Run();





