namespace UXAV.AVnet.Core.Models.Sources
{
    public static class Extensions
    {
        public static SourceCollection<T> Combine<T>(this SourceCollection<T> sources, SourceCollection<T> fromSources) where T: SourceBase
        {
            foreach (var source in fromSources)
            {
                if(sources.Contains(source.Id)) continue;
                sources.Add(source);
            }

            return sources;
        }
    }
}