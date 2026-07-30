// 1. An internal adapter that turns a Lambda into an IRunner
using TECS;
using TECS.Runner;
using TECS.Scheduler;

public class LambdaRunner : IRunner
{
    private readonly Action<LambdaRunner> _runAction;
    private IScheduler _scheduler;

    public LambdaRunner(Action<LambdaRunner> runAction)
    {
        _scheduler = new StandardSchedular();
        _runAction = runAction;
    }

    public void Run(App app)
    {
        // Simply invoke the user's lambda, passing the app in!
        _runAction(this);
    }

    public void SetSchedular(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }
}