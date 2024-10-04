using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS.Tests.Components
{
    public class TestComponents
    {
        public struct Position
        {
            public int X;
            public int Y;
        }

        public struct Velocity
        {
            public int X;
            public int Y;
        }
    }
}