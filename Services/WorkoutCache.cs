public class WorkoutCache
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly WorkoutQueries _queries;
    private List<WorkoutRecord> _workouts = [];
    private bool _isLoaded;

    public WorkoutCache(WorkoutQueries queries)
    {
        _queries = queries;
    }

    public async Task<List<WorkoutRecord>> Get()
    {
        if (_isLoaded)
        {
            return _workouts;
        }

        await _gate.WaitAsync();
        try
        {
            if (!_isLoaded)
            {
                _workouts = (await _queries.GetAllAsync()).ToList();
                _isLoaded = true;
            }

            return _workouts;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateCache()
    {
        _workouts = [];
        _isLoaded = false;
    }
}
