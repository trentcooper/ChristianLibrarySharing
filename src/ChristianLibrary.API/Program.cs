using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();  // Adds UI at /scalar/v1
}

// Redirect root to Scalar UI in development
app.MapGet("/", () => app.Environment.IsDevelopment() 
    ? Results.Redirect("/scalar/v1") 
    : Results.Ok(new { message = "Christian Library Sharing API", version = "v1.0" }));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();