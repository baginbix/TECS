using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECS.ECS.Tests.Performance
{
    public class PerformanceTest
    {
        class TestClass{
            public int Id { get; set;}
        }

        [Benchmark]
        public void InsertI32()
        {
            ECS app = new ECS();
            for (int i = 0; i < 1_000_000; i++) {
                Entity id  = app.CreateEntity();
                app.InsertComponent(id,i);
            }
        }


        [Benchmark]
        public void InsertStruct()
        {
            ECS app = new ECS();
            for (int i = 0; i < 1_000_000; i++) {
                Entity id  = app.CreateEntity();
                //app.InsertComponent(id,new Position{});
            }
        }

        [Benchmark]
        public void InsertClass()
        {
            ECS app = new ECS();
            for (int i = 0; i < 1_000_000; i++) {
                Entity id  = app.CreateEntity();
                app.InsertComponent(id,new TestClass{});
            }
        }



    }
}
