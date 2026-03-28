public class WorkoutCache
{
    private List<WorkoutRecord> _workouts = [];    
    private WorkoutQueries _queries;

    public WorkoutCache(WorkoutQueries queries)
    {
        _queries = queries;
    }

    public async Task<List<WorkoutRecord>> Get()
    {
        if (_workouts.Count != await _queries.CountAllAsync())
        {
            _workouts = (await _queries.GetAllAsync()).ToList();
        }

        return _workouts;
    }

    public void InvalidateCache()
    {
        _workouts.Clear();
    }
}
