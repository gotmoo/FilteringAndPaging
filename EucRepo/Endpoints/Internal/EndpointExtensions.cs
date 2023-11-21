using System.Reflection;

namespace EucRepo.Endpoints.Internal;

public static class EndpointExtensions
{
    public static void UseEndpoints<TMarker>(this IApplicationBuilder app)
    {
        UseEndpoints(app, typeof(TMarker));
    }
    public static void UseEndpoints(this IApplicationBuilder app, Type typeMarker)
    {
        var endpointTypes = GetEndpointTypesFromAssemblyContaining(typeMarker);
        foreach (var endpointType in endpointTypes)
        {
            endpointType.GetMethod(nameof(IEndpoints.DefineEndpoint))!
                .Invoke(null, new object[] {app});
        }
    }
    
    private static IEnumerable<TypeInfo> GetEndpointTypesFromAssemblyContaining(Type typeMarker)
    {
        return typeMarker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract && !x.IsInterface &&
                        typeof(IEndpoints).IsAssignableFrom(x));
    }
}