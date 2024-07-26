/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2024-07-25
 * 
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Text.Json;

using Seventy.GameTheory.PlayingField.Model;
using Seventy.GameTheory.PlayingField.Extensions;

const string CertificateSettingsEnvironmentVariable = "CERTIFICATE_SETTINGS";
const string ApiPortEnvironmentVariable = "PLAYING_FIELD_PORT";

const string CorsPolicyName = "ClientPolicy";
const string HttpClientName = "CustomHttpClient";

var builder = WebApplication.CreateBuilder(args);

// Get the certificate settings from the environment variable
var certSettings = JsonSerializer.Deserialize<CertificateSettings>(
    Environment.GetEnvironmentVariable(CertificateSettingsEnvironmentVariable) ?? throw new InvalidOperationException($"{CertificateSettingsEnvironmentVariable} environment variable not set"),
    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
)!;

// Get the API port from the environment variable
var apiPort = int.Parse(Environment.GetEnvironmentVariable(ApiPortEnvironmentVariable) ?? throw new InvalidOperationException($"{ApiPortEnvironmentVariable} environment variable not set"));

// Configure the Kestrel server with the certificate and the API port
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(apiPort, listenOptions =>
{
    listenOptions.UseHttps(new X509Certificate2($"{certSettings.Path}.pfx", certSettings.Password));
    // Enable HTTP/2 and HTTP/1.1 for gRPC-Web compatibility
    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
}));

// Allow all origins
builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, policyBuilder =>
{
    policyBuilder
        // Allow all ports on localhost
        .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
        // Allow all methods and headers
        .AllowAnyMethod()
        .AllowAnyHeader()
        // Expose the gRPC-Web headers
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
}));

builder.Services.AddGrpc();

builder.Services.AddSingleton(certSettings);

// Add custom HttpClient with the certificate handler to talk to the gRPC services
// - The certificate is loaded from the environment variable and does not need to be installed on the machine
// - First we need to create a factory for the HttpClient with the custom handler
builder.Services.AddHttpClient(HttpClientName).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
{
    var certificateSettings = serviceProvider.GetRequiredService<CertificateSettings>();
    var logger = serviceProvider.GetRequiredService<ILogger<GeneralLogContext>>();

    // Load the certificate from the environment variable
    var certificate = new X509Certificate2($"{certificateSettings.Path}.crt");

    // Expected thumbprint and issuer of the certificate for validation
    var expectedThumbprint = certificate.Thumbprint;
    var expectedIssuer = certificate.Issuer;

    logger.LogInformation("Creating custom HttpClient with certificate handler for {0}", expectedIssuer);

    // Create the gRPC channels and clients with the custom certificate handler
    var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(certificate);

    handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) =>
        cert?.Issuer == expectedIssuer && cert.Thumbprint == expectedThumbprint;

    return handler;
});

// Add the custom HttpClient to the service provider
// - The HttpClient is created using the factory with the custom handler
builder.Services.AddTransient(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var logger = serviceProvider.GetRequiredService<ILogger<GeneralLogContext>>();

    logger.LogInformation("Creating custom HttpClient with certificate handler");

    return httpClientFactory.CreateClient(HttpClientName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable the HTTPS redirection - only use HTTPS
app.UseHttpsRedirection();

// Enable CORS - allow all origins and add gRPC-Web headers
app.UseCors(CorsPolicyName);

// Enable gRPC-Web for all services
app.UseGrpcWeb(new() { DefaultEnabled = true });

// Add all services in the Services namespace
app.MapGrpcServices();

app.Run();

// Dummy class for logging
public class GeneralLogContext { }