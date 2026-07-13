namespace Defra.Lis.Be4Fe.Api.Utils.Mongo;

using MongoDB.Bson.Serialization.Conventions;

public static class MongoConventions
{
    private static int s_initialized;

    public static void Register()
    {
        if (Interlocked.Exchange(ref s_initialized, 1) == 1)
        {
            return;
        }

        var conversions = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };

        ConventionRegistry.Register("CamelCase", conversions, _ => true);
    }
}