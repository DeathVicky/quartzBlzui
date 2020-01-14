using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;
using QM.Service;
using Autofac;
using QM.Utility;
using QM.Utility.Extensions;
using AutoMapper;
using QM.Utility.Quartz;
using System.Collections.Generic;
using QM.Interface;
using QM.Blazor.ServerRender;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace QM.BlazorAdmin
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConnectionFactory.Init(connstr => configuration[connstr]);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutoMapperConfig));
            services.AddMemoryCache();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();//ע��ISchedulerFactory��ʵ����
            services.AddDbContext<DocsDbContext>(options =>
            {
                options.UseInMemoryDatabase("docs");
            });
            services.AddBlazAdmin();
            //services.Configure<AppConfig>(Configuration.GetSection("MyConfig"));


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ISchedulerFactory schedulerFactory, IQuartzService quartzService, IMapper mapper)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseResponseCaching();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseLoggingMiddleware();
            app.UseRouting();
            //app.UseAuthentication();//��֤
            //app.UseAuthorization();//��Ȩ
            app.UseBlazAdmin();

            #region �������ʾ��
            //QuartzOption options1 = new QuartzOption()
            //{
            //    Describe = "��ʼ��������",
            //    ExecuteType = ExecuteType.Asb,
            //    GroupName = "Demo",
            //    Interval = "ss,20",
            //    IntervalType = IntervalType.Simple,
            //    LastRunTime = DateTime.Now,
            //    TaskName = "Demo��������Simple",
            //    TaskTarget = "QM.Utility,QM.Utility.Quartz.MyFirstJob",
            //    RunTimes = 0,
            //    TaskData = new { a = 1, b = "2", c=new { x=new Random().Next(Environment.TickCount) } }
            //};
            //QuartzOption options2 = new QuartzOption()
            //{
            //    Describe = "��ʼ��������",
            //    ExecuteType = ExecuteType.Get,
            //    GroupName = "Demo",
            //    Interval = "0/20 * * * * ? *",
            //    IntervalType = IntervalType.Cron,
            //    LastRunTime = DateTime.Now,
            //    TaskName = "Demo��������Cron",
            //    TaskTarget = "http://121.31.13.132:10000/api/values/1",
            //    RunTimes = 0,
            //    TaskData = new { a = 1, b = "2", c = new { x = new Random().Next(Environment.TickCount) } }
            //};
            //app.UseCustomQuartz(p=>p.AddJobListener(new CustomJobListener()),new List<QuartzOption>() { options1,options2 });
            var result = quartzService.ExecuteSql("update quartz set  TaskStatus=0  ");
            var jobs = quartzService.QueryAsync(p => p.TaskStatus == 0).Result;
            if (jobs != null && jobs.Count() > 0)
            {
                List<QuartzOption> quartzModels = mapper.Map<List<QuartzOption>>(jobs);
                //quartzModels.Reverse()
                app.UseCustomQuartz(p => p.AddJobListener(new CustomJobListener()), quartzModels);
            }

            //_ = schedulerFactory.AddJob<MySecondJob>(new QuartzOption()
            //{
            //    RunTimes = 0,
            //    Interval = "ss,1",
            //    IntervalType = IntervalType.Simple,
            //    TaskData = "MySecondJobData",
            //    TaskName = "MySecondJob",
            //    GroupName = "Demo"
            //}).Result;//���������UseCustomQuartz֮����ӣ�scheduler--->null
            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
      

        }

        /// <summary>
        /// ϵͳ����
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<CustomAutofacModule>();
        }
      
    }
}
