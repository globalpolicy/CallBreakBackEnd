
using CallBreakBackEnd.Hubs;
using CallBreakBackEnd.Models;
using CallBreakBackEnd.Services.Impl;
using CallBreakBackEnd.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CallBreakBackEnd
{
	public class Program
	{
		public static void Main(string[] args)
		{

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddDbContextPool<AppDbContext>(optionsBuilder =>
			{
				optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("CallBreakDb"));
			});

			builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
				{
					ClockSkew = TimeSpan.Zero,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:PrivateSigningKey"])),
					ValidIssuer = builder.Configuration["JWT:Issuer"],
					ValidAudience = builder.Configuration["JWT:Audience"],
					ValidateIssuerSigningKey = true,
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
				};
			});

			builder.Services.AddSingleton<ITokenService, TokenService>();
			builder.Services.AddScoped<IGameService, GameService>();
			builder.Services.AddSingleton<IClientNotifierService, ClientNotifierService>();
			builder.Services.AddSingleton<IConnectionIdPlayerMapperService, ConnectionIdPlayerMapperService>();

			builder.Services.AddSignalR(); // ref: https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-8.0&WT.mc_id=dotnet-35129-website&tabs=visual-studio

			// Define the CORS policy
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowSpecificOrigin",
					policy =>
					{
						policy.WithOrigins("https://callbreak.ajashra.com") // "http://127.0.0.1:5500", "http://localhost:5500", "http://localhost"
							  .AllowAnyMethod()
							  .AllowAnyHeader()
							  .AllowCredentials(); // apparently needed for SignalR
					});
			});

			builder.Services.AddControllers();
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

			// Use the CORS policy
			app.UseCors("AllowSpecificOrigin"); // this should be before app.UseAuthorization()

			app.UseAuthorization();



			app.MapControllers();

			app.MapHub<GameHub>("/gamehub");

			app.Run();


		}
	}
}
