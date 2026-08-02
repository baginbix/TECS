// 1. An internal adapter that turns a Lambda into an IRunner
using TECS;
using TECS.Runner;
using TECS.Scheduler;

public class LambdaRunner : IRunner
{
    private readonly Action<App> _runAction;

    public LambdaRunner(Action<App> runAction)
    {
        _runAction = runAction;
    }

    public void Run(App app)
    {
        // Simply invoke the user's lambda, passing the app in!
        _runAction(app);
    }
}