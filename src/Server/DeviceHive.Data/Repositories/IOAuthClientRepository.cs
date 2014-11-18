using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IOAuthClientRepository : ISimpleRepository<OAuthClient>
    {
        List<OAuthClient> GetAll(OAuthClientFilter filter = null);
        OAuthClient Get(string oauthId);
    }

    public static class OAuthClientRepositoryExtension
    {
        public static IQueryable<OAuthClient> Filter(this IQueryable<OAuthClient> query, OAuthClientFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Name != null)
                query = query.Where(e => e.Name == filter.Name);

            if (filter.NamePattern != null)
                query = query.Where(e => e.Name.Contains(filter.NamePattern));

            if (filter.Domain != null)
                query = query.Where(e => e.Domain == filter.Domain);

            if (filter.OAuthID != null)
                query = query.Where(e => e.OAuthID == filter.OAuthID);

            if (filter.SortField != OAuthClientSortField.None)
            {
                switch (filter.SortField)
                {
                    case OAuthClientSortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case OAuthClientSortField.Name:
                        query = query.OrderBy(e => e.Name, filter.SortOrder);
                        break;
                    case OAuthClientSortField.Domain:
                        query = query.OrderBy(e => e.Domain, filter.SortOrder);
                        break;
                    case OAuthClientSortField.OAuthID:
                        query = query.OrderBy(e => e.OAuthID, filter.SortOrder);
                        break;
                }
            }

            if (filter.Skip != null)
                query = query.Skip(filter.Skip.Value);

            if (filter.Take != null)
                query = query.Take(filter.Take.Value);

            return query;
        }
    }
}
