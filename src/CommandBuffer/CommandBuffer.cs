using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TECS.Commands;

//TODO: fix how fake ids work, they should take into account that inserting into real entities is a possibility
public struct CommandBuffer
{
    struct CommandItem
    {
        public Type componentType;
        public ICommandQueue commandQueue;
    }
    List<Entity> entitiesToBeDestroyed;

    List<Entity> entitiesToSpawn;
    int currentFakeID = -1;
    List<CommandItem> componentCommandQueues;
    List<Entity> mappedIDs;
    public CommandBuffer()
    {
        entitiesToBeDestroyed = new(100);
        entitiesToSpawn = new(100);
        componentCommandQueues = new(100);
        mappedIDs = new(100);
    }

    private Entity GenerateID(){
        return currentFakeID--;
    }

    public void DestroyEntity(Entity id){
        entitiesToBeDestroyed.Add(id);
    }

    public Entity SpawnEntity()
    {
        var entity = GenerateID();
        entitiesToSpawn.Add(entity);
        return entity;
    }

    public void InsertComponent<T>(T component, Entity entity)
    where T : struct
    {
        var queue = GetOrCreateCommandQueue<T>();
        queue.InsertComponent(component, entity);
    }

    public void RemoveComponent<T>(Entity entity)
    where T : struct
    {
    #if DEBUG
        if (entity.Id < 0)
        {
            throw new InvalidOperationException(
                $"Attempted to remove {typeof(T).Name} from a 'Fake' Entity (ID: {entity.Id}). " +
                "If you just spawned this entity, you can't do this. This behavior is not yet supported");
        }
    #endif
        var queue = GetOrCreateCommandQueue<T>();
        queue.RemoveComponent(entity);
    }

    public void Flush(ECS ecs){
        for(int i = 0; i < entitiesToSpawn.Count; i++){
            mappedIDs.Add(ecs.CreateEntity());
        }
        entitiesToSpawn.Clear();

        foreach(var queue in componentCommandQueues){
            queue.commandQueue.Flush(ecs, CollectionsMarshal.AsSpan(mappedIDs));
        }

        foreach(var entity in entitiesToBeDestroyed){
            ecs.DestroyEntity(entity);
        }
        entitiesToBeDestroyed.Clear();

        currentFakeID = -1;
        mappedIDs.Clear();
    }


    private ComponentQueue<T> GetOrCreateCommandQueue<T>()
    where T : struct{
        Type componentType = typeof(T);
        foreach(var item in componentCommandQueues){
            if(item.componentType == componentType){
                return (ComponentQueue<T>)item.commandQueue;
            }
        }
        var newQueue = new ComponentQueue<T>();
        var commandItem = new CommandItem(){
            componentType = componentType,
            commandQueue = newQueue
        };
        componentCommandQueues.Add(commandItem);
        return newQueue;
    }
}
