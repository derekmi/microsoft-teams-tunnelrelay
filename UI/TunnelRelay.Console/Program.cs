﻿// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Console
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Extensions.CommandLineUtils;
    using Microsoft.Extensions.Options;
    using TunnelRelay.Core;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Program execution class.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// Program invocation point.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        /// <returns>Process return code.</returns>
        public static int Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(false);
            CommandOption serviceBusUrlOption = commandLineApplication.Option(
                "-BusUrl | --ServiceBusUrl",
                "Url for the Service bus you want to use. This should be in format sbname.servicebus.windows.net",
                CommandOptionType.SingleValue);

            CommandOption serviceBusSharedKeyNameOption = commandLineApplication.Option(
                "-KeyName | --ServiceBusKeyName",
                "Name of the shared key. For example RootManageSharedAccessKey",
                CommandOptionType.SingleValue);

            CommandOption serviceBusSharedKeyOption = commandLineApplication.Option(
                "-Key | --ServiceBusKey",
                "Shared access key. This key should have Manage, Send and Listen permissions",
                CommandOptionType.SingleValue);

            CommandOption connectionNameOption = commandLineApplication.Option(
                "-Name | --ConnectionName",
                "Unique hybrid connection name, This connection should already be created.",
                CommandOptionType.SingleValue);

            CommandOption serviceAddressOption = commandLineApplication.Option(
                "-Address | --ServiceAddress",
                "Endpoint to route requests to. Example http://localhost:4200",
                CommandOptionType.SingleValue);

            commandLineApplication.HelpOption("-h|--help|-?");
            commandLineApplication.OnExecute(() =>
            {
                string serviceBusUrl = serviceBusUrlOption.Value();
                string sharedKeyName = serviceBusSharedKeyNameOption.Value();
                string sharedKey = serviceBusSharedKeyOption.Value();
                string connectionName = connectionNameOption.Value();
                string serviceAddress = serviceAddressOption.Value();

                bool paramsPresent = true;
                if (string.IsNullOrEmpty(serviceBusUrl))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus url");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(sharedKeyName))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus shared key name");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(sharedKey))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus shared key");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(connectionName))
                {
                    System.Console.Error.WriteLine("Missing required hybrid connection name");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(serviceAddress))
                {
                    System.Console.Error.WriteLine("Missing required service url");
                    paramsPresent = false;
                }

                if (!paramsPresent)
                {
                    return -1;
                }

                HybridConnectionManagerOptions hybridConnectionManagerOptions = new HybridConnectionManagerOptions
                {
                    ConnectionPath = connectionName,
                    ServiceBusKeyName = sharedKeyName,
                    ServiceBusSharedKey = sharedKey,
                    ServiceBusUrlHost = serviceBusUrl,
                };

                RelayRequestManager relayManager = new RelayRequestManager(
                    new SimpleOptionsMonitor<RelayRequestManagerOptions>
                    {
                        CurrentValue = new RelayRequestManagerOptions
                        {
                            InternalServiceUrl = new Uri(serviceAddress),
                        },
                    },
                    new List<ITunnelRelayPlugin>(),
                    new RelayRequestEventListener());

                HybridConnectionManager hybridConnectionManager = new HybridConnectionManager(
                    Options.Create(hybridConnectionManagerOptions),
                    relayManager);

                hybridConnectionManager.InitializeAsync(CancellationToken.None).Wait();

                System.Console.CancelKeyPress += (sender, cancelledEvent) =>
                {
                    hybridConnectionManager.CloseAsync(CancellationToken.None).Wait();
                    System.Environment.Exit(0);
                };

                // Prevents this host process from terminating so relay keeps running.
                Thread.Sleep(Timeout.Infinite);
                return 0;
            });

            return commandLineApplication.Execute(args);
        }
    }
}