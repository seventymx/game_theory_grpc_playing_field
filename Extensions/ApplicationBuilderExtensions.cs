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

using System.Reflection;

namespace Seventy.GameTheory.PlayingField.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string ServiceNamespace = ".Services";

    public static IApplicationBuilder MapGrpcServices(this IApplicationBuilder app, string serviceNamespace = ServiceNamespace)
    {
        // Get the base namespace of the assembly
        var baseNamespace = Assembly.GetExecutingAssembly().GetName().Name;

        // Combine the base namespace with the service namespace
        var serviceTypeNamespace = $"{baseNamespace}{ServiceNamespace}";

        // Get all service types in the assembly
        var serviceTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.Namespace?.StartsWith(serviceTypeNamespace) == true);

        // Filter out nested types
        // - we only want the top-level service types (gRPC services)
        serviceTypes = serviceTypes.Where(x => !x.IsNested);

        // Map each service type
        foreach (var serviceTypeToMap in serviceTypes)
        {
            var method = typeof(GrpcEndpointRouteBuilderExtensions)
                .GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))
                !.MakeGenericMethod(serviceTypeToMap);

            _ = method.Invoke(null, [app]);
        }

        return app;
    }
}