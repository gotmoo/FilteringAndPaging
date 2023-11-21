using EucRepo.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EucRepo.Helpers;

public static class DbContextExtensions
{
    public static IEntityType GetTableInfo<TEntity>(this DbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        if (entityType is null)
        {
            throw new ArgumentException($"The type '{typeof(TEntity)}' is not found");
        }

        return entityType;
    }  
}