using System.Web;
using EucRepo.Endpoints.Internal;
using EucRepo.Helpers;
using EucRepo.Persistence;

namespace EucRepo.Endpoints;

public class DirectorViewEndpoint : IEndpoints
{
    public static void DefineEndpoint(IEndpointRouteBuilder app)
    {

        app.MapGet("/api/DirectorView", new Func<SqlDbContext, string, string, string, object>(
            (SqlDbContext db, string id, string farm, string item) => ResolveDirectorUrl(db, farm, id, item)));
    }

    private static object ResolveDirectorUrl(SqlDbContext db, string farm, string id, string item)
    {
        try
        {
            string urlBuilder = string.Empty;
            /*var siteInfo = db.InfraSites.First(i => i.Name == farm);
            var directorSite = siteInfo.DirectorSite;*/
            switch (id.ToLower())
            {
                case "session":
                    /*urlBuilder = directorSite + "/Director/default.html?locale=en_US#HELP_DESK&" +
                                 HelperMethods.ResolveSid(item) +
                                 "&" +
                                 HttpUtility.UrlEncode(
                                     item); //item.Split(new[] { "\\" }, StringSplitOptions.None)[1]*/
                    break;

                case "computer":
                    /*urlBuilder = directorSite + "/Director/default.html?locale=en_US#MACHINE_DETAILS&" +
                                 siteInfo.ConfigurationServiceGroupUid + "&" + HelperMethods.ResolveSid(item + "$") + "&" +
                                 HttpUtility.UrlEncode(
                                     item); //item.Split(new[] { "\\" }, StringSplitOptions.None)[1]*/
                    break;

                default:
                    return Results.BadRequest("Unable to retrieve metadata.");
            }

            return Results.Redirect(urlBuilder);
        }
        catch (Exception)
        {
            return Results.BadRequest("Unable to retrieve metadata.");
        }
    }
}