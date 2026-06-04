using Application;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using WebApi;
using WebApi.Hubs;
using Hangfire;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:5001"; // URL вашего Identity проекта
        options.Audience = "resource_api";
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = delegate { return true; }
        };

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
       
            ValidateAudience = true,
            ValidAudience = "resource_api",
            RoleClaimType = "role",
            NameClaimType = "name"
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Поставь сюда точку останова (breakpoint)
                Console.WriteLine("Ошибка аутентификации: " + context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Регистрируем MediatR-хендлеры из WebApi (EntityChangedHandler и др.)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Clear();
    });


var MyAllowSpecificOrigins = "AllowAngularSPA";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:4200") // URL вашего Angular
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        if (context.Description.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            var hasAllowAnonymous = actionDescriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any()
                                 || actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

            if (hasAllowAnonymous)
            {
                // ИСПРАВЛЕНИЕ: Инициализируем список, если он null, перед очисткой
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Clear();
            }
        }
        return Task.CompletedTask;
    });

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {



        document.Info.Title = "AutoPanel API";
        document.Info.Version = "v1";
        document.Info.Description = "API для работы с AutoPanel";
        // 1. Инициализация компонентов
        if (document.Components == null)
        {
            document.Components = new OpenApiComponents();
        }

        // 2. Описываем схему
        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Введите JWT токен"
        };

        // ИСПРАВЛЕНИЕ: Приведение к интерфейсу IOpenApiSecurityScheme
        // Используем явное приведение словаря или просто добавляем через интерфейс
        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }

        document.Components.SecuritySchemes["Bearer"] = (IOpenApiSecurityScheme)securityScheme;

        // 3. Глобальное требование авторизации
        if (document.Security == null)
        {
            document.Security = new List<OpenApiSecurityRequirement>();
        }

        var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
        var requirement = new OpenApiSecurityRequirement();

        // Передаем пустой список строк для Scopes
        requirement.Add(schemeReference, new List<string>());

        document.Security.Add(requirement);

        return Task.CompletedTask;
    });
});

var app = builder.Build();
app.UseExceptionHandler(); // Это активирует ваш GlobalExceptionHandler

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // <-- 2. Создает UI по адресу /scalar/v1
}

 app.UseHttpsRedirection();   //          Будет нужно

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
app.MapControllers();
app.MapHub<EntityHub>("/hubs/entities");

app.Run();
