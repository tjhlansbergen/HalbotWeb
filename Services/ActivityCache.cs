public class ActivityCache
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ActivityQueries _queries;
    private List<HalbotActivity> _activities = [];
    private bool _isLoaded;

    public ActivityCache(ActivityQueries queries)
    {
        _queries = queries;
    }

    public async Task<List<HalbotActivity>> Get()
    {
        if (_isLoaded)
        {
            return _activities;
        }

        await _gate.WaitAsync();
        try
        {
            if (!_isLoaded)
            {
                _activities = ActivityTranslators.Parse(await _queries.GetAllAsync());
                _isLoaded = true;
            }

            return _activities;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateCache()
    {
        _activities = [];
        _isLoaded = false;
    }
}