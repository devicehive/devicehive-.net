using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;
using System.Linq;

namespace DeviceHive.Data.Repositories
{
    public interface IAccessKeyRepository : ISimpleRepository<AccessKey>
    {
        List<AccessKey> GetByUser(int userId, AccessKeyFilter filter = null);
        List<AccessKey> GetByUsers(int[] userIds, AccessKeyFilter filter = null);
        AccessKey Get(string key);
        void Cleanup(DateTime timestamp);
    }

    public static class AccessKeyRepositoryExtension
    {
        public static IQueryable<AccessKey> Filter(this IQueryable<AccessKey> query, AccessKeyFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Label != null)
                query = query.Where(e => e.Label == filter.Label);

            if (filter.LabelPattern != null)
                query = query.Where(e => e.Label.Contains(filter.LabelPattern));

            if (filter.Type != null)
                query = query.Where(e => e.Type == filter.Type);

            if (filter.SortField != AccessKeySortField.None)
            {
                switch (filter.SortField)
                {
                    case AccessKeySortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case AccessKeySortField.Label:
                        query = query.OrderBy(e => e.Label, filter.SortOrder);
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
