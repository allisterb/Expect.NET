﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExpectNet
{
    public class Expect
    {
        public static Session Spawn(ISpawnable spawnable, string line_terminator, CancellationToken ct) 
        {
            spawnable.Init();
            return new Session(spawnable, line_terminator, ct);
        }
    }
}
