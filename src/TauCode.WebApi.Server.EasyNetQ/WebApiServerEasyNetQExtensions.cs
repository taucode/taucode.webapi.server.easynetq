using Autofac;
using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TauCode.Cqrs.Mq;
using TauCode.Mq;
using TauCode.Mq.Abstractions;
using TauCode.Mq.EasyNetQ;

namespace TauCode.WebApi.Server.EasyNetQ
{
    public static class WebApiServerEasyNetQExtensions
    {
        public static ContainerBuilder AddEasyNetQPublisher(
            this ContainerBuilder containerBuilder,
            Type domainEventConverterType,
            string connectionString)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (domainEventConverterType != null && !domainEventConverterType.IsAssignableTo<IDomainEventConverter>())
            {
                throw new ArgumentException(
                    $"'{nameof(domainEventConverterType)}' must be either null or implement interface '{typeof(IDomainEventConverter).FullName}'.",
                    nameof(domainEventConverterType));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            containerBuilder
                .RegisterType<EasyNetQMessagePublisher>()
                .As<IMessagePublisher>()
                .WithProperties(new List<Parameter>
                {
                    new NamedPropertyParameter(nameof(EasyNetQMessagePublisher.ConnectionString), connectionString),
                })
                .SingleInstance();

            if (domainEventConverterType != null)
            {
                containerBuilder
                    .RegisterType(domainEventConverterType)
                    .As<IDomainEventConverter>()
                    .SingleInstance();
            }

            return containerBuilder;
        }

        public static ContainerBuilder AddEasyNetQSubscriber(
            this ContainerBuilder containerBuilder,
            Assembly[] handlersAssemblies,
            Type messageHandlerContextFactoryType,
            string connectionString)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (handlersAssemblies == null)
            {
                throw new ArgumentNullException(nameof(handlersAssemblies));
            }

            if (handlersAssemblies.Any(x => x == null))
            {
                throw new ArgumentException($"'{nameof(handlersAssemblies)}' cannot contain nulls.");
            }

            if (messageHandlerContextFactoryType == null)
            {
                throw new ArgumentNullException(nameof(messageHandlerContextFactoryType));
            }

            if (!messageHandlerContextFactoryType.IsAssignableTo<IMessageHandlerContextFactory>())
            {
                throw new ArgumentException(
                    $"'{nameof(messageHandlerContextFactoryType)}' must implement interface '{typeof(IMessageHandlerContextFactory).FullName}'.",
                    nameof(messageHandlerContextFactoryType));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            containerBuilder
                .RegisterAssemblyTypes(handlersAssemblies)
                .Where(x => x.IsClosedTypeOf(typeof(IMessageHandler<>)))
                .AsSelf()
                .InstancePerLifetimeScope();

            containerBuilder
                .RegisterType(messageHandlerContextFactoryType)
                .As<IMessageHandlerContextFactory>()
                .SingleInstance();

            containerBuilder
                .RegisterType<EasyNetQMessageSubscriber>()
                .As<IMessageSubscriber>()
                .WithProperties(new List<Parameter>
                {
                    new NamedPropertyParameter(nameof(EasyNetQMessageSubscriber.ConnectionString), connectionString),
                })
                .SingleInstance();

            return containerBuilder;
        }

        public static IList<Type> GetRegisteredMessageHandlerTypes(this ILifetimeScope scope)
        {
            var list = new List<Type>();

            foreach (var registration in scope.ComponentRegistry.Registrations)
            {
                var services = registration.Services;
                foreach (var service in services)
                {
                    if (service is TypedService typedService)
                    {
                        var serviceType = typedService.ServiceType;
                        if (serviceType.IsClosedTypeOf(typeof(IMessageHandler<>)))
                        {
                            list.Add(serviceType);
                        }
                    }
                }
            }

            return list;
        }
    }
}
