namespace EucRepo.Endpoints.Internal;

public interface IEndpoints
{
    public static abstract void DefineEndpoint(IEndpointRouteBuilder app);
}