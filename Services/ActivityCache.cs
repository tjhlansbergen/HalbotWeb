public class ActivityCache
{
    private List<HalbotActivity> _activities = [];    
    private ActivityQueries _queries;

    public ActivityCache(ActivityQueries queries)
    {
        _queries = queries;
    }

    public async Task<List<HalbotActivity>> Get()
    {
        if (_activities.Count != await _queries.CountAllAsync())
        {
            _activities = ActivityTranslators.Parse(await _queries.GetAllAsync());
        }

        return _activities;
    }

    public void InvalidateCache()
    {
        _activities.Clear();
    }
}