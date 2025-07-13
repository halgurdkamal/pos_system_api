using pos_system_api.data;

var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.UseUrls("http://*:8080");


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
