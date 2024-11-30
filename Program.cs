using Microsoft.Azure.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
if (builder.Environment.IsProduction())
{
    // Use ManagedIdentity to authenticate with Azure Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["AzureRedis:ConnectionString"];
        options.InstanceName = "PixelBoard_";
    });
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379"; // Replace with your Redis connection string
        options.InstanceName = "PixelBoard_";
    });
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapHub<BoardHub>("/boardHub");

app.Run();
