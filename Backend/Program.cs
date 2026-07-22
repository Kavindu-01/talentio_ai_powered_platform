using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Web.WebView2.Wpf;
using Backend.Data;
using Backend.Services;

namespace DesktopApp
{
    public class Program
    {
        private static IHost? _aspnetHost;

        [STAThread]
        public static void Main(string[] args)
        {
            // 1. Launch ASP.NET Core API server on a background thread
            var serverThread = new Thread(() => RunServer(args));
            serverThread.IsBackground = true;
            serverThread.SetApartmentState(ApartmentState.MTA);
            serverThread.Start();

            // 2. Launch the WPF application and WebView2 desktop window on the main STA thread
            var app = new Application();
            var window = new Window
            {
                Title = "Talentio - Recruitment and Talent Management Platform",
                Width = 1024,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Root Grid Layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar row
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // WebBrowser row

            // Slate Gray control toolbar border container (handles background and padding)
            var toolbarBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)), // Slate 900
                Padding = new Thickness(12, 6, 12, 6)
            };

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            toolbarBorder.Child = toolbar;

            // Title Label / Drag Handle
            var titleLabel = new TextBlock
            {
                Text = "Talentio Desktop Control Panel  |  ",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            toolbar.Children.Add(titleLabel);

            // Control Buttons
            var minBtn = new Button { Content = " Minimize ", Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(6, 2, 6, 2) };
            minBtn.Click += (s, e) => window.WindowState = WindowState.Minimized;
            toolbar.Children.Add(minBtn);

            var maxBtn = new Button { Content = " Maximize / Restore ", Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(6, 2, 6, 2) };
            maxBtn.Click += (s, e) => window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            toolbar.Children.Add(maxBtn);

            var resetBtn = new Button { Content = " Reset Size (1024x700) ", Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(6, 2, 6, 2) };
            resetBtn.Click += (s, e) =>
            {
                window.WindowState = WindowState.Normal;
                window.Width = 1024;
                window.Height = 700;
                window.Left = (SystemParameters.PrimaryScreenWidth - 1024) / 2;
                window.Top = (SystemParameters.PrimaryScreenHeight - 700) / 2;
            };
            toolbar.Children.Add(resetBtn);

            // Drag handle binding: Click-and-drag toolbar border to move the window, double click to maximize
            toolbarBorder.MouseDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    if (e.ClickCount == 2)
                    {
                        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    }
                    else
                    {
                        window.DragMove();
                    }
                }
            };

            Grid.SetRow(toolbarBorder, 0);
            mainGrid.Children.Add(toolbarBorder);

            var webView = new WebView2();
            Grid.SetRow(webView, 1);
            mainGrid.Children.Add(webView);

            window.Content = mainGrid;

            window.Loaded += async (sender, e) =>
            {
                try
                {
                    // Ensure the Webview2 runtime is loaded
                    await webView.EnsureCoreWebView2Async(null);
                    
                    // Clear browser cache on startup to avoid cached React bundles
                    try
                    {
                        await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
                    }
                    catch { }

                    // Navigate to the ASP.NET Core server
                    webView.Source = new Uri("http://localhost:5267");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to initialize the embedded browser component: {ex.Message}\n\nPlease ensure Microsoft Edge WebView2 Runtime is installed.", "Browser Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Shut down ASP.NET server when closing the desktop UI window
            window.Closed += (sender, e) =>
            {
                try
                {
                    _aspnetHost?.StopAsync().Wait();
                }
                catch { }
                Environment.Exit(0);
            };

            app.Run(window);
        }

        private static void RunServer(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Services.AddControllers()
                    .AddJsonOptions(options => {
                        options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    });
                builder.Services.AddEndpointsApiExplorer();

                // Swagger configuration with JWT support
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Recruitment and Talent Management API",
                        Version = "v1"
                    });
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter valid JWT Token",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "Bearer"
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                            Array.Empty<string>()
                        }
                    });
                });

                // Configure Database Connection (SQLite)
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=recruitment.db"));

                // Configure JWT Authentication
                var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_FOR_RECRUITMENT_PLATFORM_2026";
                var key = Encoding.ASCII.GetBytes(jwtKey);

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RecruitmentAPI",
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RecruitmentClient",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

                // Configure CORS
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowReact", policy =>
                    {
                        policy.WithOrigins("http://localhost:5173") // Vite React Port
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

                // Register custom services
                builder.Services.AddHttpClient<IAIService, AIService>();
                builder.Services.AddSingleton<INotificationService, NotificationService>();
                builder.Services.AddSingleton<ICalendarService, CalendarService>();
                builder.Services.AddSingleton<IStorageService, StorageService>();

                var app = builder.Build();
                _aspnetHost = app;

                // Auto create and seed SQLite Database on startup
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                }

                // Ensure Uploads folder exists
                var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Serve React Static SPA Files from wwwroot with Cache-Control headers
                app.UseDefaultFiles();
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        var headers = ctx.Context.Response.Headers;
                        headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                        headers["Pragma"] = "no-cache";
                        headers["Expires"] = "0";
                    }
                });

                // Serve static files from Uploads folder (resumes, etc.)
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(uploadsPath),
                    RequestPath = "/Uploads"
                });

                // Configure HTTP pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseCors("AllowReact");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
                app.MapFallbackToFile("index.html");

                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API server failed to start: {ex.Message}", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
