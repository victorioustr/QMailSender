using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using QMailSender;
using QMailSender.Authorization;
using QMailSender.Entities;
using QMailSender.Handlers.Commands;
using QMailSender.Handlers.Validations;
using QMailSender.Helpers;
using QMailSender.Services;
using QMailSender.Services.QueueService;

var securityScheme = new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security"
};

var securityReq = new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] { }
    }
};

var contact = new OpenApiContact
{
    Name = "Muzaffer AKYIL",
    Email = "m.akyil@qt.net.tr",
    Url = new Uri("https://qt.net.tr")
};

var license = new OpenApiLicense
{
    Name = "Free License",
    Url = new Uri("https://qt.net.tr")
};

var info = new OpenApiInfo
{
    Version = "v1",
    Title = "QMailSender - Queueble Mail Sender API",
    Description = "",
    TermsOfService = new Uri("https://qt.net.tr"),
    Contact = contact,
    License = license
};

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
var services = builder.Services;
var env = builder.Environment;

services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("QMailSenderDatabase")));
services.AddCors();
services.AddControllers()
    .AddNewtonsoftJson(x =>
    {
        x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });
;
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
services.AddScoped<IJwtUtils, JwtUtils>();
services.AddScoped<IUserService, UserService>();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", info);
    o.AddSecurityDefinition("Bearer", securityScheme);
    o.AddSecurityRequirement(securityReq);
});
services.AddHostedService<SenderWorker>();
services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity)) queueCapacity = 100;

    return new DefaultBackgroundTaskQueue(queueCapacity);
});
services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<SendCommandValidator>();
var app = builder.Build();

// add hardcoded test user to db on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    var appSettings = builder.Configuration
        .GetSection("AppSettings")
        .Get<AppSettings>();

    if (!context.Users.Any())
    {
        var user = new User
        {
            FirstName = appSettings.DefaultFirstName,
            LastName = appSettings.DefaultLastName,
            Username = appSettings.DefaultUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(appSettings.DefaultPassword)
        };
        context.Users.Add(user);
        context.SaveChanges();
    }
}

app.UseCors(x => x
    .SetIsOriginAllowed(origin => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

// global error handler
app.UseMiddleware<ErrorHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
// custom jwt auth middleware
app.UseMiddleware<JwtMiddleware>();
app.MapControllers();

Jobs.Configure(app.Services.GetService<IMediator>());

app.Run();