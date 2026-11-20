using System.Collections.ObjectModel;

namespace NyxMonitor.App.ViewModels;

internal static class CollectionSync
{
    /// <summary>
    /// Updates <paramref name="target"/> in place so bindings keep their elements.
    /// The collection is only rebuilt when the set of items changes (different count or identity).
    /// </summary>
    public static void Sync<TItem, TSource>(
        ObservableCollection<TItem> target,
        IReadOnlyList<TSource> source,
        Func<TItem, TSource, bool> sameItem,
        Func<TSource, TItem> create,
        Action<TItem, TSource> update)
    {
        bool same = target.Count == source.Count;
        for (int i = 0; same && i < source.Count; i++)
        {
            same = sameItem(target[i], source[i]);
        }

        if (!same)
        {
            target.Clear();
            foreach (TSource item in source)
            {
                target.Add(create(item));
            }
        }

        for (int i = 0; i < source.Count; i++)
        {
            update(target[i], source[i]);
        }
    }
}
