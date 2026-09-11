using HomeNotes.Core.Interfaces;
using HomeNotes.Core.Services;
using HomeNotes.Infrastucture.Data;
using HomeNotes.Infrastucture.Services;
using HomeNotes.Infrastucture.StorageFiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("NpgsqlConnection"));
});

builder.Services.AddScoped<IUserStore, UserStore>();
builder.Services.AddScoped<INotesStore, NotesStore>();
builder.Services.AddScoped<IAttachmentsStore, AttachmentsStore>();
builder.Services.AddScoped<IHashPassword, HashPassword>();
builder.Services.AddSingleton<IFileStore>(
    new LocalFileStorage(builder.Configuration["Storage:RootPath"] ?? builder.Environment.ContentRootPath));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotesService, NotesService>();
builder.Services.AddScoped<IAttachmentsService, AttachmentsService>();
builder.Services.AddScoped<IGetUserCurrentId, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //dbContext.Database.EnsureDeleted();
    //dbContext.Database.EnsureCreated();
    //dbContext.Database.Migrate();
   
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();