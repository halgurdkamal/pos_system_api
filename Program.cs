using pos_system_api.data;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for IIS hosting
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// builder.WebHost.UseUrls("http://*:8080");

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS
app.UseCors();

// Comment out HTTPS redirection for hosting compatibility
// app.UseHttpsRedirection();

app.MapGet("/drug/{id}", (string id) =>
{

    return SampleDrugProvider.GetDrug() ?? new Dictionary<string, object>
    {
        { "error", "No drug data available" }
    };
});

// list of drugs
app.MapGet("/drugs", () =>
{
    return SampleDrugProvider.GetDrugList();
});



app.MapGet("/", () =>
{

return "Welcome to the POS System API! Use /drugs to get sample drug data.";
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
