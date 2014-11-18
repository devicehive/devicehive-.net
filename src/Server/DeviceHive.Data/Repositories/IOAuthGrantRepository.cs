using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IOAuthGrantRepository : ISimpleRepository<OAuthGrant>
    {
        List<OAuthGrant> GetByUser(int userId, OAuthGrantFilter filter = null);
        OAuthGrant Get(Guid authCode);
    }

    public static class OAuthGrantRepositoryExtension
    {
        public static IQueryable<OAuthGrant> Filter(this IQueryable<OAuthGrant> query, OAuthGrantFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Start != null)
            {
                var start = DateTime.SpecifyKind(filter.Start.Value, DateTimeKind.Utc);
                query = query.Where(e => e.Timestamp >= start);
            }

            if (filter.End != null)
            {
                var end = DateTime.SpecifyKind(filter.End.Value, DateTimeKind.Utc);
                query = query.Where(e => e.Timestamp <= end);
            }

            if (filter.ClientID != null)
                query = query.Where(e => e.ClientID == filter.ClientID.Value);

            if (filter.ClientOAuthID != null)
                query = query.Where(e => e.Client.OAuthID == filter.ClientOAuthID);

            if (filter.Type != null)
                query = query.Where(e => e.Type == filter.Type.Value);

            if (filter.Scope != null)
                query = query.Where(e => e.Scope == filter.Scope);

            if (filter.RedirectUri != null)
                query = query.Where(e => e.RedirectUri == filter.RedirectUri);

            if (filter.AccessType != null)
                query = query.Where(e => e.AccessType == filter.AccessType.Value);

            if (filter.SortField != OAuthGrantSortField.None)
            {
                switch (filter.SortField)
                {
                    case OAuthGrantSortField.Timestamp:
                        query = query.OrderBy(e => e.Timestamp, filter.SortOrder);
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
