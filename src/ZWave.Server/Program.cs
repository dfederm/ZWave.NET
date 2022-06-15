using ZWave;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(
    options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "hh:mm:ss ";
    });

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSingleton<Driver>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Driver>>();
    string portName = "COM3"; // TODO: Configure or make an argument
    return Driver.CreateAsync(logger, portName, CancellationToken.None).GetAwaiter().GetResult();
});

var app = builder.Build();

// Initialize the driver at startup to ensure the server doesn't start without a working one.
_ = app.Services.GetRequiredService<Driver>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
