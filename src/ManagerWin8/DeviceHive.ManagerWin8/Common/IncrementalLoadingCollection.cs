using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DeviceHive.ManagerWin8.Common
{
    public class IncrementalLoadingCollection<EntityType> : ObservableCollection<EntityType>, ISupportIncrementalLoading
    {
        public delegate Task<List<EntityType>> LoadItems(uint count = 0, uint offset = 0);
        public event Action<IncrementalLoadingCollection<EntityType>, bool> IsLoadingChanged;

        bool isLoading = false;
        bool hasMoreItems = true;
        uint minItemsLoadCount;
        uint offset = 0;
        uint lastOffsetDelta = 0;
        LoadItems loadItems;

        public IncrementalLoadingCollection(LoadItems loadItems, uint minItemsLoadCount = 12)
        {
            this.loadItems = loadItems;
            this.minItemsLoadCount = minItemsLoadCount;
        }

        public IncrementalLoadingCollection(LoadItems loadItems, IEnumerable<EntityType> collection, uint minItemsLoadCount = 12)
            : base(collection)
        {
            this.loadItems = loadItems;
            this.minItemsLoadCount = minItemsLoadCount;
            offset = (uint)collection.Count();
        }

        public bool WasAnySuccessLoading
        {
            get
            {
                return Count > 0 || !HasMoreItems;
            }
        }

        public bool HasMoreItems
        {
            get
            {
                return hasMoreItems;
            }
            protected set
            {
                hasMoreItems = value;
            }
        }

        public uint MinItemsLoadCount
        {
            get
            {
                return minItemsLoadCount;
            }
            set
            {
                if (value < 1)
                {
                    return;
                }
                minItemsLoadCount = value;
            }
        }

        void OnIsLoadingChanged(IncrementalLoadingCollection<EntityType> list, bool isLoading)
        {
            if (IsLoadingChanged != null)
            {
                IsLoadingChanged.Invoke(list, isLoading);
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            count = Math.Max(count, minItemsLoadCount);
            CoreDispatcher dispatcher = Window.Current.Dispatcher;

            return Task.Run<LoadMoreItemsResult>(
                async () =>
                {
                    if (isLoading)
                    {
                        return new LoadMoreItemsResult() { Count = 0 };
                    }
                    isLoading = true;

                    if (IsLoadingChanged != null)
                    {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            OnIsLoadingChanged(this, true);
                        });
                    }

                    List<EntityType> list = null;
                    bool wasException = false;
                    try
                    {
                        list = await loadItems(count, offset);
                    }
                    catch
                    {
                        wasException = true;
                        HasMoreItems = false;
                    }

                    if (!wasException || IsLoadingChanged != null)
                    {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if (!wasException)
                            {
                                if (list.Count == 0)
                                {
                                    HasMoreItems = false;
                                    offset -= lastOffsetDelta;
                                }
                                else
                                {
                                    HasMoreItems = true;
                                    offset += count;
                                    lastOffsetDelta = (uint)(count - list.Count);
                                    foreach (EntityType item in list)
                                    {
                                        Add(item);
                                    }
                                }
                            }
                            OnIsLoadingChanged(this, false);
                        });
                    }

                    isLoading = false;
                    return new LoadMoreItemsResult() { Count = !wasException ? (uint)list.Count : 0 };
                }).AsAsyncOperation<LoadMoreItemsResult>();
        }

        protected override void InsertItem(int index, EntityType item)
        {
            base.InsertItem(index, item);

            offset++;
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            offset--;
            HasMoreItems = true;
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            offset = 0;
            lastOffsetDelta = 0;
            HasMoreItems = true;
        }
    }
}
