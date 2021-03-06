﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Options;
using FirstService.Implementations;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace FirstService
{
    public class Startup
    {
        public IContainer ApplicationContainer { get; private set; }
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            //it can removes
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder(); //Autofac

            services.AddMvc();

            services.Configure<RedisOptions>(Configuration.GetSection(RedisOptions.GetConfigName));
            services.Configure<RabbitmqOptions>(Configuration.GetSection(RabbitmqOptions.GetConfigName));
            services.Configure<MongoOptions>(Configuration.GetSection(MongoOptions.GetConfigName));
            services.Configure<OAuthOptions>(Configuration.GetSection(OAuthOptions.GetConfigName));
            services.Configure<FirstServiceOptions>(Configuration.GetSection(FirstServiceOptions.GetConfigName));

            ConfigureRedis(services);
            ConfigureDistributedCache(services);

            services.GeneralServices(Configuration);
            builder.AutofacServices(); // autofac sample DI

            services.AddSwaggerDocumentation();

            var bus = ConfigureRabbitmqHost(services);
            services.ConfigureBus(bus, Configuration);

            builder.Populate(services);
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);

        }
        public virtual void ConfigureRedis(IServiceCollection services)
        {
            string redisHost = Configuration[$"{RedisOptions.GetConfigName}:{nameof(RedisOptions.host)}"];
            services.AddScoped(provider => ConnectionMultiplexer.Connect(redisHost).GetDatabase());
        }

        public virtual void ConfigureDistributedCache(IServiceCollection services)
        {
            string redisHost = Configuration[$"{RedisOptions.GetConfigName}:{nameof(RedisOptions.host)}"];
            string redisName = Configuration[$"{RedisOptions.GetConfigName}:{nameof(RedisOptions.name)}"];

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = redisHost;
                options.InstanceName = redisName;
            });
        }

        public virtual IBusControl ConfigureRabbitmqHost(IServiceCollection services)
        {
            var bus = services.ServiceBusRabbitmqConfiguration(Configuration);
            return bus;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/api/Home/Error");

            app.UseSwaggerDocumentation();

            app.UseAuthentication();

            app.UseMvc();

            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }
    }
}
