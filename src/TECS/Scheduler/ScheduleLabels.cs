namespace TECS.Scheduler.Labels;

// Runs once when application starts
public struct Startup;

// Runs every time the ECS runs
public struct PreUpdate;

//Runs every time the ECS runs
public struct Update;

//Runs every time the ECS runs
public struct PostUpdate;

// Transisions states like this OnEnter -> OnUpdate -> OnExit
public struct StateTransition;
