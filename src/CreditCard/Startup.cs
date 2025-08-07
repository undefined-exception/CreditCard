using CreditCard.Data;
using CreditCard.Extensions;
using CreditCard.Models;
using CreditCard.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CreditCard
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetValue<string>("SQLConnectionString");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews();

            services.AddApplicationInsightsTelemetry();

            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddHostedService<ApplicationStatusUpdateService>();
            services.AddScoped<IApplicationNotificationService, ApplicationNotificationService>();

            var configsection = Configuration.GetSection(nameof(CreditBureauApiConfig));
            services.AddHttpClientWithPolicies<IApplicationApiService, ApplicationApiService, CreditBureauApiConfig>(configsection);
        }

        public void Configure(
                        IApplicationBuilder app,
                        IWebHostEnvironment env,
                        IOptions<DefaultUserConfig> apiConfiguration)
        {
            if (env.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Application}/{action=List}/{id?}");

                endpoints.MapRazorPages();
            });

        }
    }
}