using PKHeX.Rest;
using PKHeX.Rest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddScoped<SaveFileService>();
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new RawRequestBodyFormatter());
});
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

